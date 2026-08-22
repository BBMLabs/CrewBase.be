using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Npgsql;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Infrastructure.Idempotency;
using RowingClub.BuildingBlocks.Infrastructure.Observability;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;
using RowingClub.BuildingBlocks.Infrastructure.Tenancy;
using StackExchange.Redis;

namespace RowingClub.BuildingBlocks.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the cross-cutting infrastructure shared by every module: correlation id, Redis
    /// (idempotency), and the single PostgreSQL/EF Core DbContext + unit-of-work/migration
    /// machinery every module's repositories build on.
    /// </summary>
    public static IServiceCollection AddRowingClubInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<ICorrelationIdAccessor, AsyncLocalCorrelationIdAccessor>();

        services
            .AddOptions<RedisOptions>()
            .Bind(configuration.GetSection(RedisOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            var redisOptions = provider.GetRequiredService<IOptions<RedisOptions>>().Value;

            var connectionOptions = new ConfigurationOptions
            {
                // MediatR builds every registered IPipelineBehavior<,> for EVERY request, so this
                // gets constructed even for commands that never touch idempotency.
                // AbortOnConnectFail must be false or a Redis outage would take down completely
                // unrelated commands (e.g. Register) instead of only the idempotency check that
                // actually needs Redis.
                AbortOnConnectFail = false,
                User = redisOptions.Username,
                Password = redisOptions.Password,
            };
            connectionOptions.EndPoints.Add(redisOptions.Host, redisOptions.Port);

            return ConnectionMultiplexer.Connect(connectionOptions);
        });

        services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();

        services
            .AddOptions<PostgresOptions>()
            .Bind(configuration.GetSection(PostgresOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<PostgresOptions>>().Value;
            return NpgsqlDataSource.Create(options.ConnectionString);
        });

        services.AddDbContext<RowingClubDbContext>((provider, options) =>
            options.UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>()));

        services.AddScoped<ITenantDatabase, CurrentTenantDatabase>();

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddHostedService<EfMigrationHostedService>();

        services
            .AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres");

        return services;
    }
}
