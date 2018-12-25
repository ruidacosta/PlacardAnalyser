using System.Collections.Generic;

namespace PlacardAnalyser.Analyser
{
    public interface IBet
    {
        //List<Event> BetEvents { get; set; }
        decimal CalcFinalPrice();
        decimal CalcFinalReturn();
        decimal CalcTotalReturn();
        decimal CalcBetProbability();
        decimal CalcGainRatio();
        List<Event> GetBetEvents();
    }
}