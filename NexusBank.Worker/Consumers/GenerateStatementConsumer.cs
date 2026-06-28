 
using Azure.Storage.Blobs;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NexusBank.Worker.Data;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusBank.Worker.Consumers
{
    public class CustomerProfile
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
    }

    public class GenerateStatementConsumer : IConsumer<NexusBank.Contracts.GenerateStatementRequest>
    {
        private readonly ILogger<GenerateStatementConsumer> _logger;
        private readonly string _sqlConnectionString;
        private readonly string _mongoConnectionString;
        private readonly string _blobConnectionString;
        private readonly LedgerDbContext _dbContext;

        public GenerateStatementConsumer(
            ILogger<GenerateStatementConsumer> logger,
            IConfiguration configuration,
            LedgerDbContext dbContext)
        {
            _logger = logger;
            _sqlConnectionString = configuration.GetConnectionString("DefaultConnection") ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;
            _mongoConnectionString = configuration["CosmosDb__ConnectionString"] ?? Environment.GetEnvironmentVariable("CosmosDb__ConnectionString") ?? "mongodb://nexus-nosql:27017";
            _blobConnectionString = configuration["AzureStorage__ConnectionString"] ?? Environment.GetEnvironmentVariable("AzureStorage__ConnectionString") ?? "UseDevelopmentStorage=true;";
            _dbContext = dbContext;
        }

        public async Task Consume(ConsumeContext<NexusBank.Contracts.GenerateStatementRequest> context)
        {
            var message = context.Message;
            _logger.LogInformation("[WORKER] Started statement processing for Account: {AccountId} | Period: {Period}", message.AccountId, message.StatementPeriod);

            try
            {
                // 1. FETCH CUSTOMER PROFILE FROM MONGO (NoSQL Tier)
                var mongoClient = new MongoClient(_mongoConnectionString);
                var mongoDatabase = mongoClient.GetDatabase("nexusbank-nosql-hb");

                var collectionNames = await (await mongoDatabase.ListCollectionNamesAsync()).ToListAsync();
                if (!collectionNames.Contains("CustomerProfiles"))
                {
                    _logger.LogWarning("[WORKER DIAGNOSTIC] 'CustomerProfiles' missing in Cosmos DB. Forcing creation...");
                    await mongoDatabase.CreateCollectionAsync("CustomerProfiles");
                }

                var customerCollection = mongoDatabase.GetCollection<CustomerProfile>("CustomerProfiles");
                var customer = await customerCollection.Find(c => c.Id == message.AccountId).FirstOrDefaultAsync();

                if (customer == null)
                {
                    customer = new CustomerProfile
                    {
                        Id = message.AccountId,
                        Name = "Valued Customer",
                        Email = "client@nexusbank.com"
                    };

                    await customerCollection.InsertOneAsync(customer);
                    _logger.LogInformation("[WORKER SUCCESS] Seeded record to online Cosmos DB collection.");
                }

                // 2. PARSE TARGET STATEMENT PERIOD
                // Dynamically handles formats like "2026-06"
                if (!DateTime.TryParse($"{message.StatementPeriod}-01", out DateTime targetPeriod))
                {
                    targetPeriod = new DateTime(2026, 06, 01); // Fallback safe default
                }

                // 3. FETCH REAL TRANSACTIONS FROM SQL SERVER USING ACTUAL LEDGER COLUMNS
                var transactions = await _dbContext.Transactions
                    .Where(t => (t.SenderAccountId == message.AccountId || t.ReceiverAccountId == message.AccountId)
                                && t.Timestamp.Year == targetPeriod.Year
                                && t.Timestamp.Month == targetPeriod.Month)
                    .ToListAsync();

                // 4. COMPILE STATEMENT DOCUMENT
                var statementData = new StringBuilder();
                statementData.AppendLine("=========================================================================");
                statementData.AppendLine($"                          NEXUS BANK STATEMENT                          ");
                statementData.AppendLine("=========================================================================");
                statementData.AppendLine($"Account ID:       {customer.Id}");
                statementData.AppendLine($"Customer Name:    {customer.Name}");
                statementData.AppendLine($"Email Address:    {customer.Email}");
                statementData.AppendLine($"Statement Period: {message.StatementPeriod}");
                statementData.AppendLine($"Generated On:     {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                statementData.AppendLine("-------------------------------------------------------------------------");
                statementData.AppendLine("Timestamp           | Type   | Counterparty         | Amount             ");
                statementData.AppendLine("-------------------------------------------------------------------------");

                decimal runningBalance = 0;
                foreach (var tx in transactions)
                {
                    // Calculate transaction direction relative to this account request
                    bool isSender = tx.SenderAccountId == message.AccountId;
                    string typeIndicator = isSender ? "DEBIT " : "CREDIT";
                    string counterParty = isSender ? tx.ReceiverAccountId : tx.SenderAccountId;

                    // Adjusted amount calculation based on cashflow direction
                    decimal displayAmount = isSender ? -tx.Amount : tx.Amount;
                    runningBalance += displayAmount;

                    // 🌟 FIXED: Replaced old .Date and .Description references with verified schema fields
                    statementData.AppendLine($"{tx.Timestamp:yyyy-MM-dd HH:mm:ss} | {typeIndicator} | {counterParty,-20} | {displayAmount,13:N2} {tx.Currency}");
                }

                statementData.AppendLine("-------------------------------------------------------------------------");
                statementData.AppendLine($"Ending Balance Change for Period:                        {runningBalance,13:N2}");
                statementData.AppendLine("=========================================================================");

                // 5. UPLOAD STATEMENT TO AZURE BLOB STORAGE
                var blobServiceClient = new BlobServiceClient(_blobConnectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient("statements");

                await containerClient.CreateIfNotExistsAsync();

                string uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
                string fileName = $"{message.AccountId}_{message.StatementPeriod}_{uniqueId}.txt";
                var blobClient = containerClient.GetBlobClient(fileName);

                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(statementData.ToString())))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                _logger.LogInformation("[WORKER SUCCESS] Statement compiled and uploaded cleanly to cloud storage as: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WORKER CRITICAL FAULT] Failed to generate statement for Account: {AccountId}", message.AccountId);
                throw;
            }
        }
    }
}