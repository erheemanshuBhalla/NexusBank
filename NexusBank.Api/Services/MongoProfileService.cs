namespace NexusBank.Api.Services
{
    using MongoDB.Driver;
    using NexusBank.Api.Interface;
    using NexusBank.Api.Models;

    public class MongoProfileService : ICustomerProfileService
    {
        private readonly IMongoCollection<CustomerProfile> _profilesCollection;

        public MongoProfileService(IConfiguration configuration)
        {
            // 1. Direct environment variable lookup (Highest Priority for Docker)
            var connectionString = Environment.GetEnvironmentVariable("CosmosDb__ConnectionString");

            // 2. If null, fall back to standard C# Configuration providers
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = configuration["CosmosDb:ConnectionString"]
                                ?? configuration["CosmosDb__ConnectionString"];
            }

            // 3. Ultimate local development fallback
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "mongodb://nexus-nosql:27017/NexusBankDb";
            }

            // 💡 Let's print the actual resolved string to the console logs so we can verify it
            Console.WriteLine($"[NEXUS-NOSQL-STARTUP] Connecting using string: {connectionString}");

            var mongoUrl = new MongoUrl(connectionString);
            var client = new MongoClient(mongoUrl);
            var database = client.GetDatabase(mongoUrl.DatabaseName ?? "NexusBankDb");

            _profilesCollection = database.GetCollection<CustomerProfile>("CustomerProfiles");
        }

        public async Task CreateProfileAsync(CustomerProfile profile) =>
            await _profilesCollection.InsertOneAsync(profile);

        public async Task<CustomerProfile?> GetProfileAsync(string customerId) =>
            await _profilesCollection.Find(p => p.CustomerId == customerId).FirstOrDefaultAsync();
    }
}
