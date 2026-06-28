namespace NexusBank.Contracts
{
    public record GenerateStatementRequest
    {
        public string AccountId { get; init; }
        public string Name { get; init; }
        public string Email { get; init; }
        public string StatementPeriod { get; init; } = null!; // 🌟 Added so the worker knows the timeframe!
    }
}