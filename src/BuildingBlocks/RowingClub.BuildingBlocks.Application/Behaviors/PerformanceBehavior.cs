using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace RowingClub.BuildingBlocks.Application.Behaviors;

/// <summary>Warns when a handler exceeds the expected latency budget (spec section 19 - Metric: Request süresi).</summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly TimeSpan SlowThreshold = TimeSpan.FromMilliseconds(500);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        if (stopwatch.Elapsed > SlowThreshold)
        {
            logger.LogWarning(
                "Yavaş işlem: {RequestName} {ElapsedMilliseconds} ms sürdü",
                typeof(TRequest).Name, stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}
