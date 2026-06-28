using Azure.Storage.Blobs;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using NexusBank.Api.Events;
using NexusBank.Contracts;

[ApiController]
[Route("api/[controller]")]
public class StatementsController : ControllerBase
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly string _blobConnectionString;

   
    // 💡 Notice we removed BlobStatementStorageService from here entirely!
    public StatementsController(IPublishEndpoint publishEndpoint, IConfiguration configuration)
    {
        _publishEndpoint = publishEndpoint;
        _blobConnectionString = configuration["AzureStorage__ConnectionString"]!;
    }

    [HttpGet("download/{accountId}/{period}/{uniqueId}")]
    public async Task<IActionResult> DownloadStatement(string accountId, string period, string uniqueId)
    {
        var blobServiceClient = new BlobServiceClient(_blobConnectionString);
        var containerClient = blobServiceClient.GetBlobContainerClient("statements");

        string fileName = $"{accountId}_{period}_{uniqueId}.txt";
        var blobClient = containerClient.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync())
        {
            return NotFound(new { message = "Statement file not found in cloud storage." });
        }

        var downloadInfo = await blobClient.DownloadStreamingAsync(); // 🌟 FIX

        // Streams the raw file back to Postman/Browser text formats cleanly
        return File(downloadInfo.Value.Content, "text/plain", fileName);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateStatement([FromBody] GenerateStatementDto dto)
    {
        // 🌟 Mapping properties correctly from the incoming DTO to the message contract
        await _publishEndpoint.Publish(new NexusBank.Contracts.GenerateStatementRequest
        {
            AccountId = dto.AccountId,
            StatementPeriod = dto.StatementPeriod,
            Name = "Customer", // Hardcoded or pulled from user claims/db context
            Email = "customer@nexusbank.com" // Hardcoded or pulled from user claims/db context
        });

        return Ok(new { message = "Statement generation request queued successfully." });
    }
}

public class GenerateStatementDto
{
    public string AccountId { get; set; } = null!;
    public string StatementPeriod { get; set; } = null!;
}