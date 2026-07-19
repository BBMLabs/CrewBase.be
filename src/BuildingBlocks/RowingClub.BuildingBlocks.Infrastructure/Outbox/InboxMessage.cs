namespace RowingClub.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// One row per successfully-processed <see cref="OutboxMessage.Id"/> a consumer has seen, on a
/// unique index. A consumer checks this collection before acting on a message and inserts into it
/// atomically with its own side effect, so redelivery (at-least-once by design) never double-applies
/// an event (spec section 21 - "Consumer idempotent olmalıdır", "İşlenmiş mesaj ID'leri saklanmalıdır").
/// No consumer exists yet (Notifications module is still scaffold-only) - this is the shape the
/// first one will use.
/// </summary>
public sealed class InboxMessage
{
    public Guid EventId { get; private set; }

    public required string ConsumerName { get; init; }

    public DateTimeOffset ProcessedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    private InboxMessage()
    {
    }

    public static InboxMessage Create(Guid eventId, string consumerName) =>
        new() { EventId = eventId, ConsumerName = consumerName };
}
