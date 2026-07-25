using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres;

/// <summary>
/// Applies every pending EF Core migration at startup - replaces the old <c>IMongoMigration</c>/
/// <c>MongoMigrationRunner</c> machinery with EF's own built-in migration history table.
/// Hosted services are singletons, but <see cref="RowingClubDbContext"/> is scoped (EF Core's
/// default), so a scope is created explicitly rather than injecting the context directly.
/// </summary>
public sealed class EfMigrationHostedService(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<RowingClubDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
