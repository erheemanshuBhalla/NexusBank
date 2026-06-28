namespace NexusBank.Api.Models;

public class Account
{
    public int Id { get; set; }

    // The unique public-facing string identifier (e.g., "ACC-123456")
    public string AccountNumber { get; set; } = string.Empty;

    // Financial numbers MUST use decimal to avoid floating-point errors
    public decimal Balance { get; set; }

    public string Currency { get; set; } = "USD";

    // Tracks ownership back to a specific bank user
    public int UserId { get; set; }
}

public class TransactionLedger
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = string.Empty; // e.g., TXN-XXXXX
    public string SenderAccountId { get; set; } = string.Empty;
    public string ReceiverAccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Settled, Failed
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}