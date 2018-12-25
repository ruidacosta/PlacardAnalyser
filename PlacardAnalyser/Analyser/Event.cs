using System;

namespace PlacardAnalyser.Analyser
{
    public class Event
    {
        public int Index { get; set; }
        public string Description { get; set; }
        public DateTime EventDateTime { get; set; }
        public string Label { get; set; }
        public decimal Price { get; set; }
        public decimal Odd 
        { 
            get { return Price; } 
        }
        public decimal Probability
        {
            get { return 1 / Odd; }
        }
        public decimal NotProbability
        {
            get {return 1 - Probability; }
        }

        public int Hours2Start()
        {
            TimeSpan date = this.EventDateTime - DateTime.Now;
            return date.Hours;
        }

        public override string ToString()
        {
            return this.Description;
        }
    }
}