using Microsoft.Extensions.Hosting;

namespace RowingClub.BuildingBlocks.Infrastructure.Mongo.Migrations;

/// <summary>Runs every registered <see cref="IMongoMigration"/> (across all modules) once, before
/// the host starts accepting requests.</summary>
public sealed class MongoMigrationHostedService(
    MongoMigrationRunner runner,
    IEnumerable<IMongoMigration> migrations)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => runner.RunAsync(migrations, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
