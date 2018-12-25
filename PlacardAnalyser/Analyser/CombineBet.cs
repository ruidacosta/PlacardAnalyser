using System.Collections.Generic;

namespace PlacardAnalyser.Analyser
{
    public class CombineBet : IBet
    {
        public List<Event> BetEvents { get; set; }
        public decimal BetValue { get; set; }

        private decimal? Probability;
        private decimal? FinalReturn;

        public CombineBet(){}

        public decimal CalcBetProbability()
        {
            if (this.Probability == null)
            {
                decimal prob = 1;
                foreach (var eventBet in BetEvents)
                {
                    prob *= eventBet.Probability;
                }
                this.Probability = prob;
            }
            return (decimal) this.Probability;
        }

        public decimal CalcFinalPrice()
        {
            return BetValue;
        }

        public decimal CalcFinalReturn()
        {
            if (this.FinalReturn == null)
            {
                decimal odd = 1;
                foreach (var eventBet in BetEvents)
                {
                    odd *= eventBet.Odd;
                }
                this.FinalReturn = odd;
            }
            return (decimal) this.FinalReturn;
        }

        public decimal CalcGainRatio()
        {
            return this.CalcFinalReturn() - 1;
        }

        public decimal CalcTotalReturn()
        {
            return this.CalcFinalReturn() * this.BetValue;
        }

        public List<Event> GetBetEvents()
        {
            return this.BetEvents;
        }
    }
}