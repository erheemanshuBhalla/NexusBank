namespace NexusBank.Api.Events;

public record StatementRequestedEvent
{
    public string AccountId { get; init; } = null!;
    public string StatementPeriod { get; init; } = null!;
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
}