using System;
using System.Collections.Generic;
using System.Linq;

namespace PlacardAnalyser.Analyser
{
    public enum MultipleBetType
    {
        //(2 of 3)(2 of 4)(3 of 4)(2 of 5)(3 of 5)(4 of 5)
        TwoOfThree,
        TwoOfFour,
        ThreeOfFour,
        TwoOFFive,
        ThreeOfFive,
        FourOfFive
    };
    public class MultipleBet : IBet
    {
        public List<Event> BetEvents { get; set; }
        public decimal BetValue { get; set; }
        public MultipleBetType BetTypeCombinations { get; set; }
        private List<List<Event>> BetCombinations;
        private Dictionary<string,decimal> PartialReturns;

        private decimal? Probability;
        private decimal? FinalReturn;

        public MultipleBet()
        {
            this.BetCombinations = new List<List<Event>>();
            this.PartialReturns = new Dictionary<string, decimal>();
        }

        public decimal CalcBetProbability()
        {
            if (this.Probability == null)
            {
                this.Probability = 1;
                foreach (var eventBet in this.BetEvents)
                {
                    this.Probability *= eventBet.Probability;
                }
            }
            return (decimal) this.Probability;
        }

        public decimal CalcFinalPrice()
        {
            return BetValue * BetCombinations.Count;
        }

        public decimal CalcFinalReturn()
        {
            if (this.FinalReturn == null)
            {
                this.FinalReturn = 0;
                foreach (var bet in this.BetCombinations)
                {
                    decimal tmpValue = 1;
                    foreach (var eventBet in bet)
                    {
                        tmpValue *= eventBet.Odd;
                    }
                    this.FinalReturn += tmpValue;
                }
            }
            return (decimal) this.FinalReturn;
        }

        public Dictionary<string,decimal> GetPartialBet()
        {
            foreach (var eventBet in this.BetEvents)
            {
                decimal tmpOdd = 0;
                var eventCombination = this.BetCombinations.Where(x => !x.Contains(eventBet));
                foreach (var combination in eventCombination)
                {
                    decimal tmpValue = 1;
                    foreach (var eventTmp in combination)
                    {
                        tmpValue *= eventTmp.Odd;
                    }
                    tmpOdd += tmpValue;
                }
                this.PartialReturns.Add(string.Format("{0} - {1} ({2})", 
                    eventBet.Index, eventBet.Description, eventBet.Label), tmpOdd);
            }
            return this.PartialReturns;
        }

        public decimal CalcGainRatio()
        {
            return this.CalcFinalReturn() - BetCombinations.Count;
        }

        public string GetCombinationTypeString()
        {
            switch (this.BetTypeCombinations)
            {
                case MultipleBetType.TwoOfThree:
                    return "2 of 3";
                case MultipleBetType.TwoOfFour:
                    return "2 of 4";
                case MultipleBetType.ThreeOfFour:
                    return "3 of 4";
                case MultipleBetType.TwoOFFive:
                    return "2 of 5";
                case MultipleBetType.ThreeOfFive:
                    return "3 of 5";
                case MultipleBetType.FourOfFive:
                    return "4 of 5";
                default:
                    return string.Empty;
            }
        }

        public decimal CalcTotalReturn()
        {
            return this.CalcFinalReturn() * BetValue;
        }

        public List<Event> GetBetEvents()
        {
            return this.BetEvents;
        }

        public void GenerateBetCombinations()
        {
            switch (this.BetTypeCombinations)
            {
                case MultipleBetType.TwoOfThree:
                    foreach (var bet in AnalyserProcessor.GetCombinations(this.BetEvents, 2))
                    {
                        this.BetCombinations.Add(bet.ToList());
                    }
                    break;
                case MultipleBetType.TwoOfFour:
                    foreach (var bet in AnalyserProcessor.GetCombinations(this.BetEvents, 2))
                    {
                        this.BetCombinations.Add(bet.ToList());
                    }
                    break;
                case MultipleBetType.TwoOFFive:
                    foreach (var bet in AnalyserProcessor.GetCombinations(this.BetEvents, 2))
                    {
                        this.BetCombinations.Add(bet.ToList());
                    }
                    break;
                case MultipleBetType.ThreeOfFour:
                    foreach (var bet in AnalyserProcessor.GetCombinations(this.BetEvents, 3))
                    {
                        this.BetCombinations.Add(bet.ToList());
                    }
                    break;
                case MultipleBetType.ThreeOfFive:
                    foreach (var bet in AnalyserProcessor.GetCombinations(this.BetEvents, 3))
                    {
                        this.BetCombinations.Add(bet.ToList());
                    }
                    break;
                case MultipleBetType.FourOfFive:
                    foreach (var bet in AnalyserProcessor.GetCombinations(this.BetEvents, 4))
                    {
                        this.BetCombinations.Add(bet.ToList());
                    }
                    break;
            }
        }
    }
}