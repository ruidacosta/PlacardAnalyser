using System.Collections.Generic;

namespace PlacardAnalyser.Analyser
{
    public class SingleBet : IBet
    {
        public Event BetEvent { get; set; }
        public decimal BetValue { get; set; }

        public SingleBet(){}

        public decimal CalcBetProbability()
        {
            return this.BetEvent.Probability;
        }

        public decimal CalcFinalPrice()
        {
            return BetValue;
        }

        public decimal CalcFinalReturn()
        {
            return this.BetEvent.Odd;
        }

        public decimal CalcGainRatio()
        {
            return this.BetEvent.Odd - 1;
        }

        public decimal CalcTotalReturn()
        {
            return this.CalcFinalReturn() * this.BetValue;
        }

        public List<Event> GetBetEvents()
        {
            return new List<Event> { this.BetEvent };
        }
    }
}