using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using NexusBank.Api.Interface;
using System.Text;
namespace NexusBank.Api.Services
{
   

    public class BlobStatementStorageService : IStatementStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private const string ContainerName = "bank-statements";

        public BlobStatementStorageService(IConfiguration configuration)
        {
            // Pulls the UseDevelopmentStorage connection string injected by Docker Compose
            var connectionString = configuration["AzureStorage:ConnectionString"]
                         ?? configuration["AzureStorage__ConnectionString"]
                         // 💡 FIX: Point explicitly to the 'nexus-storage' domain name, NOT localhost!
                         ?? "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://nexus-storage:10000/devstoreaccount1;";

            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadStatementAsync(string accountId, string statementPeriod, string textContent)
        {
            // 1. Get a reference to the container container and ensure it physically exists
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            //// 💡 Change this line to allow Public Access for Blobs only
            //await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
            await containerClient.CreateIfNotExistsAsync();

            // 2. Structure the Blob Path cleanly: account-id/year-month.txt
            string blobName = $"{accountId}/{statementPeriod}.txt";
            var blobClient = containerClient.GetBlobClient(blobName);

            // 3. Stream the raw text data payload up to the Azurite container
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(textContent));
            await blobClient.UploadAsync(stream, overwrite: true);

            // 4. Return the unique uniform resource identifier (URI) of the stored object
            return blobClient.Uri.ToString();
        }
    }
}
