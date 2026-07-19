using Microsoft.AspNetCore.Http;
using RowingClub.BuildingBlocks.Application.Abstractions;
using Serilog.Context;

namespace RowingClub.BuildingBlocks.Observability.Logging;

/// <summary>
/// Reads <c>X-Correlation-Id</c> (or generates one), stores it on <see cref="ICorrelationIdAccessor"/>
/// for the rest of the request pipeline, echoes it back on the response, and pushes it into the
/// Serilog LogContext so every log line for this request carries it (spec section 19).
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlationIdAccessor)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var headerValue)
            && !string.IsNullOrWhiteSpace(headerValue)
                ? headerValue.ToString()
                : Guid.NewGuid().ToString();

        correlationIdAccessor.Set(correlationId);

        // UseExceptionHandler() clears the response (headers included) before writing a Problem
        // Details body, which would silently drop a header set here immediately. OnStarting runs
        // right before the response actually begins sending - after any such clear/rewrite - so
        // it always wins regardless of how the request completes.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
