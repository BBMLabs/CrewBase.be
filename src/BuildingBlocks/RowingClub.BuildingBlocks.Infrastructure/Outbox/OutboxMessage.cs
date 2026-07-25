namespace RowingClub.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Written in the same PostgreSQL transaction as the business data that raised the domain event
/// (see <see cref="RowingClub.BuildingBlocks.Infrastructure.Postgres.RowingClubDbContext"/> and
/// docs/ARCHITECTURE.md). A background worker (per module, using Quartz) later reads pending
/// rows, publishes them, and marks them processed - never both in one step.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; private set; }

    public required string Type { get; init; }

    /// <summary>JSON-serialized domain event payload. Must never carry sensitive plaintext
    /// (spec section 21 - "Payload içinde hassas veri taşınmamalı veya şifrelenmelidir").</summary>
    public required string Content { get; init; }

    public required string CorrelationId { get; init; }

    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ProcessedOnUtc { get; private set; }

    public int RetryCount { get; private set; }

    public string? LastError { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage Create(string type, string content, string correlationId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            CorrelationId = correlationId,
        };

    public void MarkProcessed() => ProcessedOnUtc = DateTimeOffset.UtcNow;

    public void MarkFailed(string error)
    {
        RetryCount++;
        LastError = error;
    }
}
