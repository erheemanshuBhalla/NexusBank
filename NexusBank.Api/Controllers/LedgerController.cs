using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexusBank.Api.Data;

namespace NexusBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LedgerController : ControllerBase
{
    private readonly ILogger<LedgerController> _logger;
    private readonly BankDbContext _dbContext;

    public LedgerController(ILogger<LedgerController> logger, BankDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> ProcessLedgerTransfer([FromBody] LedgerTransferRequest request)
    {
        _logger.LogInformation("Processing transfer of {Amount} {Currency} from Account {Sender} to {Receiver}",
            request.Amount, request.Currency, request.SenderAccountId, request.ReceiverAccountId);

        // 1. Structural Validation
        if (request.Amount <= 0)
        {
            return BadRequest(new { Message = "Transaction rejected: Transfer amount must be greater than zero." });
        }

        if (request.SenderAccountId == request.ReceiverAccountId)
        {
            return BadRequest(new { Message = "Transaction rejected: Sender and receiver accounts cannot be identical." });
        }

        // 2. Open an explicit ACID Transaction Block
        using (var transaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                // 3. Fetch both accounts matching your string 'Id' property
                var senderAccount = await _dbContext.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.SenderAccountId);

                var receiverAccount = await _dbContext.Accounts
                    .FirstOrDefaultAsync(a => a.Id == request.ReceiverAccountId);

                // 4. Verify account existence
                if (senderAccount == null || receiverAccount == null)
                {
                    return NotFound(new { Message = "Transaction rejected: One or both bank accounts do not exist." });
                }

                // 5. Check for sufficient cleared funds
                if (senderAccount.Balance < request.Amount)
                {
                    return BadRequest(new { Message = "Transaction rejected: Insufficient funds in sender account." });
                }

                // 6. EXECUTE THE DOUBLE-ENTRY BALANCE (Updates in-memory tracked entities)
                senderAccount.Balance -= request.Amount; // Debit
                receiverAccount.Balance += request.Amount; // Credit

                // 7. CREATE THE AUDIT TRAIL RECORD (Matches your LedgerTransaction schema)
                var auditRow = new LedgerTransaction
                {
                    Id = Guid.NewGuid(), // Automatically assigns a unique system Guid
                    SenderAccountId = request.SenderAccountId,
                    ReceiverAccountId = request.ReceiverAccountId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Timestamp = DateTime.UtcNow
                };

                // Adds it to your actual 'Transactions' DbSet
                await _dbContext.Transactions.AddAsync(auditRow);

                // 8. Stage changes to database engine
                await _dbContext.SaveChangesAsync();

                // 🌟 COMMIT: Permanently persist to disk
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully completed transfer {TransactionId}. Funds cleared.", auditRow.Id);

                return Ok(new LedgerTransferResponse
                {
                    TransactionId = auditRow.Id.ToString(),
                    Status = "Settled",
                    Timestamp = auditRow.Timestamp,
                    Message = "Ledger updated successfully. Funds cleared."
                });
            }
            catch (Exception ex)
            {
                // 🛡️ EMERGENCY ROLLBACK
                await transaction.RollbackAsync();

                _logger.LogError(ex, "CRITICAL: Transaction failed between {Sender} and {Receiver}. Rolled back.",
                    request.SenderAccountId, request.ReceiverAccountId);

                return StatusCode(500, new { Message = "Internal banking system fault. Core ledger stabilized safely." });
            }
        }
    }
}

public record LedgerTransferRequest(
    string SenderAccountId,
    string ReceiverAccountId,
    decimal Amount,
    string Currency
);

public record LedgerTransferResponse
{
    public string TransactionId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Message { get; init; } = string.Empty;
}