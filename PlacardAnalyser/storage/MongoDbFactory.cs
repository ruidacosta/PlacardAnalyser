using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PlacardAnalyser.Analyser;

namespace PlacardAnalyser.Storage
{
    public class BetModel
    {
        public ObjectId Id { get; set; }
        [BsonElement("Type")]
        public string Type { get; set; }
        [BsonElement("Events")]
        public List<EventModel> Events { get; set; }
        [BsonElement("Odd")]
        public decimal Odd { get; set; }
        [BsonElement("Probability")]
        public decimal Probability { get; set; }
        [BsonElement("Gain")]
        public decimal Gain { get; set; }
        [BsonElement("BetValue")]
        public decimal BetValue { get; set; }
    }
    public class EventModel
    {
        [BsonElement("Index")]
        public int Index { get; set; }
        [BsonElement("EventDateTime")]
        public DateTime EventDateTime { get; set; }
        [BsonElement("Description")]
        public string Description { get; set; }
        [BsonElement("Label")]
        public string Label { get; set; }
        [BsonElement("Odd")]
        public decimal Odd { get; set; }
        [BsonElement("Probability")]
        public decimal Probability { get; set; }
    }

    public class MongoDbFactory
    {
        private readonly IMongoCollection<BetModel> _Bets;

        public MongoDbFactory(string connectionString, string _database)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(_database);
            this._Bets = database.GetCollection<BetModel>("PlacardOpenBets");
        }

        public void SaveBets(List<IBet> selectedBets)
        {
            List<BetModel> bulk = new List<BetModel>();
            foreach (var bet in selectedBets)
            {
                var betModel = new BetModel
                {
                    Type = bet is SingleBet 
                        ? "Single Bet" 
                        : bet is CombineBet
                            ? "Combined Bet"
                            : bet is MultipleBet
                                ? "Multiple Bet " + ((MultipleBet)bet).GetCombinationTypeString()
                                : "undefined",
                    Events = new List<EventModel>(),
                    Odd = bet.CalcFinalReturn(),
                    Probability = bet.CalcBetProbability(),
                    Gain = bet.CalcGainRatio(),
                    BetValue = bet.GetBetValue()
                };
                foreach (var event_ in bet.GetBetEvents())
                {
                    var eventModel = new EventModel
                    {
                        Index = event_.Index,
                        EventDateTime = event_.EventDateTime,
                        Description = event_.Description,
                        Label = event_.Label,
                        Odd = event_.Odd,
                        Probability = event_.Probability
                    };
                    betModel.Events.Add(eventModel);
                }  

                bulk.Add(betModel);              
            }
            _Bets.InsertMany(bulk);
        }
    }
}