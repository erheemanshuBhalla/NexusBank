namespace NexusBank.Contracts // Use your worker's contract namespace
{
    public record GenerateStatementRequest
    {
        public string AccountId { get; init; } = null!;
        public string StatementPeriod { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string Email { get; init; } = null!;
        public Guid CorrelationId { get; init; } = Guid.NewGuid(); // Keeps your filename unique!
    }
}