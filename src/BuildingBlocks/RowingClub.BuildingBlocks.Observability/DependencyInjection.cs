using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;
using RowingClub.BuildingBlocks.Observability.ErrorHandling;
using RowingClub.BuildingBlocks.Observability.Logging;
using RowingClub.BuildingBlocks.Observability.Telemetry;

namespace RowingClub.BuildingBlocks.Observability;

public static class DependencyInjection
{
    public static IServiceCollection AddRowingClubObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddRowingClubOpenTelemetry(configuration);

        return services;
    }

    /// <summary>Correlation id -> exception handling -> Prometheus /metrics, in that order.</summary>
    public static WebApplication UseRowingClubObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();
        app.UseHttpMetrics();
        app.MapMetrics();

        return app;
    }
}
