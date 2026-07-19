using RowingClub.BuildingBlocks.Application.Abstractions;

namespace RowingClub.BuildingBlocks.Infrastructure.Observability;

/// <summary>
/// Flows the correlation id across async continuations within one request without needing
/// HttpContext, so it is reachable from Infrastructure code (outbox writer, EF interceptors)
/// that must not depend on ASP.NET Core types.
/// </summary>
public sealed class AsyncLocalCorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> Current = new();

    public string CorrelationId => Current.Value ?? "n/a";

    public void Set(string correlationId) => Current.Value = correlationId;
}
