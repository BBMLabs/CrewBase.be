namespace RowingClub.BuildingBlocks.Application.Abstractions;

/// <summary>
/// The correlation id for the current request/flow, set once by API middleware from the
/// <c>X-Correlation-Id</c> header (or generated if absent) and read by logging, outbox messages
/// and downstream service calls so the whole chain can be traced (spec section 19).
/// </summary>
public interface ICorrelationIdAccessor
{
    string CorrelationId { get; }

    void Set(string correlationId);
}
