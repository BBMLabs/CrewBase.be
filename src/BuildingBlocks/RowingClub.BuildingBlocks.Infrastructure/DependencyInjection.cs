using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Infrastructure.Idempotency;
using RowingClub.BuildingBlocks.Infrastructure.Mongo;
using RowingClub.BuildingBlocks.Infrastructure.Mongo.Migrations;
using RowingClub.BuildingBlocks.Infrastructure.Observability;
using StackExchange.Redis;

namespace RowingClub.BuildingBlocks.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the cross-cutting infrastructure shared by every module: correlation id, Redis
    /// (idempotency), and the single MongoDB client/database + unit-of-work/migration machinery
    /// every module's repositories build on.
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

        MongoBsonConfiguration.EnsureConfigured();

        services
            .AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IMongoClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoOptions>>().Value;
            return new MongoClient(options.ConnectionString);
        });

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<MongoOptions>>().Value;
            return provider.GetRequiredService<IMongoClient>().GetDatabase(options.DatabaseName);
        });

        services.AddScoped<IMongoUnitOfWork, MongoUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<IMongoUnitOfWork>());

        services.AddSingleton<MongoMigrationRunner>();
        services.AddSingleton<IMongoMigration, CoreCollectionsMigration>();
        services.AddHostedService<MongoMigrationHostedService>();

        return services;
    }
}
