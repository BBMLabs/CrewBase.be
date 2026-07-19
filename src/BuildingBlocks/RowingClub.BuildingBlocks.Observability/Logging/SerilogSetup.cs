using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace RowingClub.BuildingBlocks.Observability.Logging;

/// <summary>
/// Structured console logging via Serilog (spec section 19). Mandatory fields (CorrelationId,
/// UserId, ClubId, Endpoint, HTTP method, status code, duration) are enriched per-request by
/// <see cref="CorrelationIdMiddleware"/> and ASP.NET Core's own request logging - never logged
/// here directly to avoid leaking secrets into a static enrichment path.
/// </summary>
public static class SerilogSetup
{
    public static WebApplicationBuilder AddRowingClubSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProperty("Application", "RowingClub.Api")
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .ReadFrom.Configuration(context.Configuration);
        });

        return builder;
    }
}
