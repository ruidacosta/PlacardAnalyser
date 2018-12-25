namespace PlacardAnalyser.Configuration
{
    public class Settings
    {
        public EmailSetts Email { get; set; }
        public BetSetts BetParams { get; set; }
        public AppSetts AppParams { get; set; }
    }

    public class EmailSetts
    {
        public string To { get; set; }
        public string From { get; set; }
        public string Smtp { get; set; }
        public int Port { get; set; }
        public string Subject { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool HtmlBody { get; set; }
        public bool AttachCSV { get; set; }
    }

    public class BetSetts
    {
        public decimal Risk { get; set; }
        public int DelayForBet { get; set; }
        public int MaxEventsPerBet { get; set; }
        public int NumberOfBets { get; set; }
        public decimal BetCash { get; set; }
        public int MaxEventsToProcess { get; set; }
    }

    public class AppSetts
    {
        public string LogsFolder { get; set; }
        public string ArchiveFolder { get; set; }
    }
}