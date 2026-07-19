namespace RowingClub.Api.HealthChecks;

public static class HealthCheckSetup
{
    public static IServiceCollection AddRowingClubHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks();
        return services;
    }

    public static WebApplication MapRowingClubHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        return app;
    }
}
