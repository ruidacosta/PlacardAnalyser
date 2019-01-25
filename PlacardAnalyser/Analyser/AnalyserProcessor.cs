using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using PlacardAnalyser.Configuration;
using PlacardAnalyser.Storage;
using PlacardAPI;

namespace PlacardAnalyser.Analyser
{
    public class AnalyserProcessor
    {
        private readonly Settings Setts;
        private static readonly ILog logger = LogManager.GetLogger(typeof(AnalyserProcessor));
        public AnalyserProcessor(Settings _setts) => Setts = _setts;

        public void Start()
        {
            // Get FullSportBook
            var fullSportBook = GetFullSportBook();

            // Get Events list based on settings parameters
            var eventsList = SelectEvents(ref fullSportBook);
            
            // Reduce events to Max Events to process (configuration)
            ReduceEvents(ref eventsList);
            // Generate bets using combinations of events
            var generatedBets = GenerateBets(ref eventsList);
            
            // Select the ones with maximum return and lower risk
            var selectedBets = SelectBets(ref generatedBets);

            // Sent selected bets throw email
            SendBets(selectedBets);

            //Store bet on Storage
            StoreBets(selectedBets);
        }

        private APIResponse GetFullSportBook()
        {
            APIResponse response = null;

            string filepath = string.Format("{0}{1}FullSportBook_{2}.json", 
                Setts.AppParams.ArchiveFolder, 
                Path.DirectorySeparatorChar,
                DateTime.Now.ToString("yyyyMMdd"));
            try
            {
                logger.InfoFormat("Getting FullSportBook...");
                APIClient client = new APIClient();
                response = client.GetFullSportsBook(filepath);
                logger.InfoFormat("FullSportBook saved to file {0}",filepath);
            }
            catch (Exception ex)
            {
                logger.Fatal(string.Format("Error getting FullSportBook"),ex);
                logger.Info("Service will finish.");
                Environment.Exit(1);
            }
            return response;
        }

        private List<Event> SelectEvents(ref APIResponse fullSportBook)
        {
            logger.Info("Selecting events...");
            List<Event> result = new List<Event>();
            int count = 0;
            foreach (var entry in fullSportBook.body.data.exportedProgrammeEntries)
            {
                string description = string.Empty;
                foreach (var descEventPath in entry.eventPaths)
                {
                    description += descEventPath.eventPathDescription + " - ";
                }
                foreach (var market in entry.markets)
                {
                    foreach (var outcome in market.outcomes)
                    {
                        if (outcome.price.decimalPrice == (decimal)0.0)
                            continue;
                        var event_tmp = new Event {
                            Index = entry.index,
                            Description = string.Format("{0}{1} ({2} - {3})",
                                description,
                                market.marketDescription,
                                entry.homeOpponentDescription,
                                entry.awayOpponentDescription),
                            EventDateTime = Convert.ToDateTime(entry.eventStartDateTime),
                            Label = outcome.outcomeDescription,
                            Price = outcome.price.decimalPrice 
                        };

                        count++;
                        if (event_tmp.NotProbability < Setts.BetParams.Risk &&
                            event_tmp.Hours2Start() > Setts.BetParams.DelayForBet)
                        {
                            result.Add(event_tmp);
                        }
                    }
                }
            }
            logger.InfoFormat("Selected events: {0} of {1}", result.Count, count);
            return result;
        }

        private void ReduceEvents(ref List<Event> eventsList)
        {
            var limitDate = DateTime.Now.Date.AddDays(1).AddHours(23).AddMinutes(59).AddSeconds(59);
            var removeCounterByTime = eventsList.RemoveAll(x => x.EventDateTime > limitDate);
            logger.InfoFormat("Removed {0} events that are not on next day.", removeCounterByTime);
            
            /*
            var tmpOdd = 1.01;
            while (eventsList.Count > Setts.BetParams.MaxEventsToProcess)
            {
               var removeCounter = eventsList.RemoveAll(x => x.Odd <= (decimal)tmpOdd); 
               tmpOdd += 0.01;
               logger.InfoFormat("{0} events removed for odd {1}", removeCounter, tmpOdd);
            }
            logger.InfoFormat("Remaining events: {0}",eventsList.Count);
            */
            
            while (eventsList.Count > Setts.BetParams.MaxEventsToProcess)
            {
                Setts.BetParams.Risk -= (decimal)0.01;
                var removeCounter = eventsList.RemoveAll(x => x.NotProbability > Setts.BetParams.Risk);
                logger.InfoFormat("{0} events removed for risk {1}", removeCounter, Setts.BetParams.Risk);
            }
            logger.InfoFormat("Remaining events: {0}",eventsList.Count);
            
        }

        private List<IBet> GenerateBets(ref List<Event> eventsList)
        {
            logger.Info("Getting Single bets...");
            List<IBet> result = GenerateSingleBets(ref eventsList);

            logger.Info("Getting Combine bets...");
            result.AddRange(GenerateCombineBets(ref eventsList));

            logger.Info("Getting Multiple bets...");
            result.AddRange(GenerateMultipleBets(ref eventsList));

            return result;
        }

        private List<IBet> GenerateSingleBets(ref List<Event> eventsList)
        {
            // one event per bet
            List<IBet> result = new List<IBet>();
            foreach (var sportEvent in eventsList)
            {
                if (sportEvent.Odd <= (decimal)1.10)
                    continue;

                var tmpBet = new SingleBet
                {
                    BetEvent = sportEvent,
                    BetValue = Setts.BetParams.BetCash
                };
                result.Add(tmpBet);
            }
            
            logger.InfoFormat("Total Single Bets generated: {0}", result.Count);
            return result;
        }

        private List<IBet> GenerateCombineBets(ref List<Event> eventsList)
        {
            List<IBet> result = new List<IBet>();
            int counter;
            for (int i=2; i <= Setts.BetParams.MaxEventsPerBet; i++)
            {
                counter = 0;
                logger.InfoFormat("Bets with {0} events",i);
                foreach (var bet in GetCombinations(eventsList, i))
                {
                    if (bet.ToList().GroupBy(x => x.Index).Where(g => g.Count() > 1).Count() > 0)
                        continue;
                    var tmpBet = new CombineBet
                    {
                        BetEvents = bet.ToList(),
                        BetValue = Setts.BetParams.BetCash
                    };
                    //logger.InfoFormat("{0}", string.Join(",", bet));
                    result.Add(tmpBet);
                    counter++;
                }
                logger.InfoFormat("{0} combined bets with {1} events", counter, i);
            }
            logger.InfoFormat("Total Combine Bets generated: {0}", result.Count);
            return result;
        }

        private List<IBet> GenerateMultipleBets(ref List<Event> eventsList)
        {
            // maximo de 5 events
            // add to bet a type (2 of 3)(2 of 4)(3 of 4)(2 of 5)(3 of 5)(4 of 5)
            List<IBet> result = new List<IBet>();
            // 3 events
            int i = 3;
            logger.InfoFormat("Bets with {0} events", i);
            foreach (var bet in GetCombinations(eventsList,i))
            {
                if (bet.ToList().GroupBy(x => x.Index).Where(g => g.Count() > 1).Count() > 0)
                    continue;
                var tmpBet = new MultipleBet
                {
                    BetEvents = bet.ToList(),
                    BetValue = Setts.BetParams.BetCash,
                    BetTypeCombinations = MultipleBetType.TwoOfThree
                };

                tmpBet.GenerateBetCombinations();

                result.Add(tmpBet);
            }

            // 4 events
            i++;
            logger.InfoFormat("Bets with {0} events", i);
            foreach (var bet in GetCombinations(eventsList,i))
            {
                if (bet.ToList().GroupBy(x => x.Index).Where(g => g.Count() > 1).Count() > 0)
                        continue;
                var tmpBet = new MultipleBet
                {
                    BetEvents = bet.ToList(),
                    BetValue = Setts.BetParams.BetCash,
                    BetTypeCombinations = MultipleBetType.TwoOfFour
                };

                tmpBet.GenerateBetCombinations();
                result.Add(tmpBet);

                var tmpBet2 = new MultipleBet
                {
                    BetEvents = bet.ToList(),
                    BetValue = Setts.BetParams.BetCash,
                    BetTypeCombinations = MultipleBetType.ThreeOfFour
                };

                tmpBet2.GenerateBetCombinations();
                result.Add(tmpBet2);
            }

            // 5 events
            i++;
            logger.InfoFormat("Bets with {0} events", i);
            foreach(var bet in GetCombinations(eventsList,i))
            {
                if (bet.ToList().GroupBy(x => x.Index).Where(g => g.Count() > 1).Count() > 0)
                        continue;
                var tmpBet = new MultipleBet
                {
                    BetEvents = bet.ToList(),
                    BetValue = Setts.BetParams.BetCash,
                    BetTypeCombinations = MultipleBetType.TwoOFFive
                };

                tmpBet.GenerateBetCombinations();
                result.Add(tmpBet);

                var tmpBet2 = new MultipleBet
                {
                    BetEvents = bet.ToList(),
                    BetValue = Setts.BetParams.BetCash,
                    BetTypeCombinations = MultipleBetType.ThreeOfFive
                };

                tmpBet2.GenerateBetCombinations();
                result.Add(tmpBet2);

                var tmpBet3 = new MultipleBet
                {
                    BetEvents = bet.ToList(),
                    BetValue = Setts.BetParams.BetCash,
                    BetTypeCombinations = MultipleBetType.FourOfFive
                };

                tmpBet3.GenerateBetCombinations();
                result.Add(tmpBet3);
            }

            logger.InfoFormat("Total Multiple Bets generated: {0}", result.Count);
            return result;
        }

        private List<IBet> SelectBets(ref List<IBet> generatedBets)
        {
            // select from bets where not probability < risk (settings)
            // Number of bets (settings) where max gain ratio 
            Dictionary<string,int> eventCounter = new Dictionary<string, int>();
            List<IBet> result = new List<IBet>();
            logger.InfoFormat("Selecting Bets...");
            var list1 = generatedBets.Where(x => (1 - x.CalcBetProbability() <= Setts.BetParams.Risk))
                .OrderByDescending(x => x.CalcGainRatio());//.Take(Setts.BetParams.NumberOfBets).ToList();
            logger.InfoFormat("Pool size: {0} bets",list1.Count());
            foreach (var bet in list1)
            {
                if (result.Count >= Setts.BetParams.NumberOfBets)
                {
                    break;
                }
                bool flag = true;
                foreach(var event_ in bet.GetBetEvents())
                {
                    var key = string.Format("{0} - {1}",event_.Index, event_.Label);
                    if (eventCounter.ContainsKey(key))
                    {
                        if (eventCounter[key] >= 2)
                        {
                            flag = false;
                            break;
                        }
                        else
                        {
                            eventCounter[key]++;
                        }
                    }
                    else
                    {
                        eventCounter.Add(key,1);
                    }
                }
                if (flag)
                {
                    result.Add(bet);
                }
            }
            return result;
        }

        private void SendBets(List<IBet> selectedBets)
        {
            // create new class to create the email and send it 
            logger.InfoFormat("Sending bets...");
            EmailFactory emailFactory = new EmailFactory(Setts.Email);
            foreach (var bet in selectedBets)
            {
                emailFactory.AddBet(bet);
            }
            if (Setts.Email.AttachCSV)
            {
                var attachCSVFile = GetCSVFile(selectedBets);
                emailFactory.SendEmail(attachCSVFile);
            }
            else
                emailFactory.SendEmail();
        }

        private string GetCSVFile(List<IBet> selectedBets)
        {
            // get file name (path)
            var filename = string.Format("{0}{1}BetSelection_{2}.csv", 
                Setts.AppParams.ArchiveFolder, 
                Path.DirectorySeparatorChar,
                DateTime.Now.ToString("yyyyMMdd"));

            GenerateCSVFile(selectedBets,filename);
            return filename;
        }

        private void GenerateCSVFile(List<IBet> selectedBets, string filename)
        {
            var csv = new StringBuilder();
            // Create table header
            var header = string.Format("Bet Type,Index,DateTime,Description,Label,Odd,Probability,Gain,Bet Value,Win / Lost,Profit,Total return");
            csv.AppendLine(header);
            string line;
            foreach (var bet_ in selectedBets)
            {
                // create csv line or lines (combined and Multiple bet)
                if (bet_ is SingleBet)
                {
                    SingleBet bet = (SingleBet) bet_;
                    line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                        "Single Bet",
                        bet.BetEvent.Index,
                        bet.BetEvent.EventDateTime,
                        bet.BetEvent.Description,
                        bet.BetEvent.Label,
                        bet.BetEvent.Odd,
                        bet.BetEvent.Probability,
                        bet.CalcGainRatio(),
                        bet.BetValue,
                        string.Empty,
                        string.Empty,
                        string.Empty); 
                    csv.AppendLine(line); 
                }
                else if (bet_ is CombineBet)
                {
                    CombineBet bet = (CombineBet) bet_;
                    foreach (var event_ in bet.BetEvents)
                    {
                        line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",
                            string.Empty,
                            event_.Index,
                            event_.EventDateTime,
                            event_.Description,
                            event_.Label,
                            event_.Odd,
                            event_.Probability,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty);
                        csv.AppendLine(line);
                    }
                    line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{9},{10},{11}",
                            "Combined Bet",
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            bet.CalcFinalReturn(),
                            bet.CalcBetProbability(),
                            bet.CalcGainRatio(),
                            bet.BetValue,
                            string.Empty,
                            string.Empty,
                            string.Empty);
                    csv.AppendLine(line);
                }
                else if (bet_ is MultipleBet)
                {
                    MultipleBet bet = (MultipleBet) bet_;
                    //Bet Type,Index,DateTime,Description,Label,Odd,Probability,Gain,Bet Value,Win / Lost,Total return
                    foreach (var event_ in bet.GetBetEvents())
                    {
                        line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{9},{10},{11}",
                            string.Empty,
                            event_.Index,
                            event_.EventDateTime,
                            event_.Description,
                            event_.Label,
                            event_.Odd,
                            event_.Probability,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty);
                        csv.AppendLine(line);
                    }
                    line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{9},{10},{11}",
                        string.Format("Multiple Bet ({0})",bet.GetCombinationTypeString()),
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        bet.CalcFinalReturn(),
                        bet.CalcBetProbability(),
                        bet.CalcGainRatio(),
                        bet.BetValue,
                        string.Empty,
                        string.Empty,
                        string.Empty);
                    csv.AppendLine(line);
                }
            }
            logger.InfoFormat("Writing csv file {0}",filename);
            File.WriteAllText(filename, csv.ToString());
        }

        public static IEnumerable<IEnumerable<Event>> GetCombinations(List<Event> items, int count)
        {
            int i = 0;
            foreach (var item in items)
            {
                if (count == 1)
                {
                    yield return new Event[] { item };
                }
                else
                {
                    foreach (var result in GetCombinations(items.Skip(i + 1).ToList(), count - 1))
                    {
                        yield return new Event[] { item }.Concat(result);
                    }
                }
                ++i;
            }
        }

        private void StoreBets(List<IBet> selectedBets)
        {
            logger.Info("Storing bets...");
            switch (Setts.Storage.Type.ToUpper())
            {
                case "MONGODB":
                    logger.Info("Storing on MongoDb database...");
                    var mongoFactory = new MongoDbFactory(Setts.Storage.ConnectionString,Setts.Storage.Database);
                    mongoFactory.SaveBets(selectedBets);
                    logger.InfoFormat("Stored on {0} in {1}"
                        ,Setts.Storage.ConnectionString
                        ,Setts.Storage.Database);
                    break;
                default:
                    break;
            }
        }
    }
}