using MongoDB.Driver;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Infrastructure.Mongo;
using RowingClub.BuildingBlocks.Infrastructure.Mongo.Migrations;
using RowingClub.Identity.Infrastructure.Persistence;
using Testcontainers.MongoDb;

namespace RowingClub.IntegrationTests.Identity;

/// <summary>
/// Testcontainers' MongoDb module ships with a single-node replica set already initiated, so
/// <see cref="MongoUnitOfWork"/>'s multi-document transactions work exactly like they would
/// against the docker-compose service (spec section 21 / docs/ARCHITECTURE.md).
/// </summary>
public sealed class MongoIdentityFixture : IAsyncLifetime
{
    private MongoDbContainer? _container;

    public IMongoDatabase Database { get; private set; } = null!;

    public IMongoUnitOfWork UnitOfWork { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new MongoDbBuilder("mongo:7").Build();
        await _container.StartAsync();

        MongoBsonConfiguration.EnsureConfigured();

        var client = new MongoClient(_container.GetConnectionString());
        Database = client.GetDatabase("rowingclub_tests");

        UnitOfWork = new MongoUnitOfWork(Database, NoopCurrentTenant.Instance, NoopCorrelationIdAccessor.Instance);

        var migrationRunner = new MongoMigrationRunner(
            Database, Microsoft.Extensions.Logging.Abstractions.NullLogger<MongoMigrationRunner>.Instance);

        await migrationRunner.RunAsync(
            [new CoreCollectionsMigration(), new IdentityCollectionsMigration()], CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    private sealed class NoopCurrentTenant : ICurrentTenant
    {
        public static readonly NoopCurrentTenant Instance = new();
        public bool IsSet => false;
        public Guid ClubId => throw new InvalidOperationException();
    }

    private sealed class NoopCorrelationIdAccessor : ICorrelationIdAccessor
    {
        public static readonly NoopCorrelationIdAccessor Instance = new();
        public string CorrelationId => "integration-test";
        public void Set(string correlationId)
        {
        }
    }
}

[CollectionDefinition(nameof(MongoIdentityCollection))]
public sealed class MongoIdentityCollection : ICollectionFixture<MongoIdentityFixture>;
