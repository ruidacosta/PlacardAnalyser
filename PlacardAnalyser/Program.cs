using System;
using PlacardAPI;

namespace PlacardAnalyser
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new APIClient();
            // client.GetFullSportsBook("FullSportsBook.json");
            // client.GetNextEvents("NextEvents.json");
            // client.GetInfo("Info.json");
            client.GetFaq("Faq.json");
        }
    }
}
