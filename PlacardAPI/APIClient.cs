using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace PlacardAPI
{
    public class APIResponse
    {
        public Header header { get; set; }
        public Body body { get; set; }
    }

    public class Header
    {
        public string timeStamp { get; set; }
        public bool responseSuccess { get; set; }
        public string version { get; set; }
    }

    public class Body
    {
        public string description { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public string status { get; set; }
        public string programmeOpenDateTime { get; set; }
        public string programmeCloseDateTime { get; set; }
        public List<ExportedProgrammeEntries> exportedProgrammeEntries { get; set; }
    }

    public class ExportedProgrammeEntries
    {
        public int index { get; set; }
        public List<EventPath> eventPaths { get; set; }
        public string eventStartDateTime { get; set; }
        public int homeOpponentId { get; set; }
        public string homeOpponentDescription { get; set; }
        public int awayOpponentId { get; set; }
        public string awayOpponentDescription { get; set; }
        public string tvChannel { get; set; }
        public bool fictional { get; set; }
        public List<Market> markets { get; set; }
        public string sportCode { get; set; }
    }

    public class EventPath
    {
        public int eventPathId { get; set; }
        public string eventPathDescription { get; set; }
        public int parentId { get; set; }
    }

    public class Market
    {
        public int index { get; set; }
        public int marketId { get; set; }
        public string marketDescription { get; set; }
        public string marketStatus { get; set; }
        public string periodDescription { get; set; }
        public string retailSalesCloseDateTime { get; set; }
        public string promotionLevel { get; set; }
    }

    public class Outcome
    {
        public int index { get; set; }
        public int outcomeId { get; set; }
        public string outcomeDescription { get; set; }
        public int handicapValue { get; set; }
        public bool hidden { get; set; }
        public bool suspended { get; set; }
        public Price price { get; set; }
        public int eventIndex { get; set; }
    }

    public class Price
    {
        public decimal decimalPrice { get; set; }
    }

    public class APIClient
    {
        private UriBuilder uriBuilder;
        private static readonly string UserAgent = "placard-api/0.2.2";
        private static readonly string ApiKey = "552CF226909890A044483CECF8196792";
        private static readonly string Channel = "1";

        // Change HttpClient to HttpWebRequest
        public APIClient()
        {
            uriBuilder = new UriBuilder("https://www.jogossantacasa.pt/");
            uriBuilder.Query = string.Format("apiKey={0}&channel={1}",ApiKey, Channel);
        }

        public APIResponse GetFullSportsBook(string file=null)
        {
            // string path = "/WebServices/SBRetailWS/FullSportsBook";
            uriBuilder.Path = "/WebServices/SBRetailWS/FullSportsBook";
            string response = DoRequest();
            if (file != null)
                Save2File(response, file);
            // convert in an object                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              
            return JsonConvert.DeserializeObject<APIResponse>(response);
        }

        public APIResponse GetNextEvents(string file=null)
        {
            uriBuilder.Path = "/WebServices/SBRetailWS/NextEvents";
            string response = DoRequest();
            if (file != null)
                Save2File(response,file);
            return JsonConvert.DeserializeObject<APIResponse>(response);
        }

        public APIResponse GetInfo(string file=null)
        {
            uriBuilder.Path = "/WebServices/ContentWS/Contents/";
            // query: { categoryCode: 'ADRETAILINFOS' }
            uriBuilder.Query += string.Format("categoryCode={0}", "ADRETAILINFOS");
            string response = DoRequest();
            if (file != null)
                Save2File(response, file);
            return JsonConvert.DeserializeObject<APIResponse>(response);
        }

        public APIResponse GetFaq(string file=null)
        {
            uriBuilder.Path = "/WebServices/ContentWS/Contents/";
            // query: { categoryCode: 'ADRETAILFAQSAPP' }
            uriBuilder.Query += string.Format("categoryCode={0}", "ADRETAILFAQSAPP");
            string response = DoRequest();
            if (file != null)
                Save2File(response, file);
            return JsonConvert.DeserializeObject<APIResponse>(response);
            
        }

        private string DoRequest()
        {
            string result = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri); 
            request.UserAgent = UserAgent;
            request.IfModifiedSince = DateTime.Now;
            request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream streamResponse = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(streamResponse);
                result = streamReader.ReadToEnd();
                streamResponse.Close();
                streamReader.Close();
                response.Close();
            }
            catch (WebException ex)
            {
                throw ex;
            }
            return result;
        }

        private void Save2File(string response, string filePath) => File.WriteAllText(filePath, response);
    }
}
