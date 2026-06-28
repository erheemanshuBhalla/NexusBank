using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace NexusBank.Api.Models
{
   

    public class CustomerProfile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // 💡 This is our Partition Key!
        [BsonElement("customerId")]
        public string CustomerId { get; set; } = null!;

        [BsonElement("firstName")]
        public string FirstName { get; set; } = null!;

        [BsonElement("lastName")]
        public string LastName { get; set; } = null!;

        [BsonElement("tier")]
        public string Tier { get; set; } = "Standard"; // Standard, Premium, VIP

        [BsonElement("dailyTransferLimit")]
        public decimal DailyTransferLimit { get; set; } = 5000.00m;
    }
}
