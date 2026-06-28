using Microsoft.EntityFrameworkCore;

namespace NexusBank.Api.Data;

public class BankDbContext : DbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<LedgerTransaction> Transactions => Set<LedgerTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Seed some dummy accounts for testing your transfers
        modelBuilder.Entity<Account>().HasData(
            new Account { Id = "ACC-SENDER-111", OwnerName = "Alice Smith", Balance = 5000.00m, Currency = "USD" },
            new Account { Id = "ACC-RECEIVER-222", OwnerName = "Bob Jones", Balance = 150.50m, Currency = "USD" }
        );
    }
}

public class Account
{
    public string Id { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
}

public class LedgerTransaction
{
    public Guid Id { get; set; }
    public string SenderAccountId { get; set; } = string.Empty;
    public string ReceiverAccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}