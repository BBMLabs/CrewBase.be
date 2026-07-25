using Microsoft.EntityFrameworkCore;
using Npgsql;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Infrastructure.Configuration;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;

namespace RowingClub.IntegrationTests.Identity;

/// <summary>
/// Connects to the same remote PostgreSQL server as local dev (.env.developer's POSTGRES_HOST/
/// PORT/USERNAME/PASSWORD - no Docker/Testcontainers), but against a dedicated
/// <c>rowingclub_tests</c> database that is dropped and recreated fresh for every test run, then
/// migrated with the real EF Core migrations so the test schema never drifts from production.
/// </summary>
public sealed class PostgresIdentityFixture : IAsyncLifetime
{
    private const string TestDatabaseName = "rowingclub_tests";

    private string _host = null!;
    private int _port;
    private string? _username;
    private string? _password;

    public RowingClubDbContext Context { get; private set; } = null!;

    public IUnitOfWork UnitOfWork { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        DotEnvFileLoader.LoadForEnvironment(
            Environment.GetEnvironmentVariable("APP_ENV") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));

        _host = Environment.GetEnvironmentVariable("POSTGRES_HOST")
            ?? throw new InvalidOperationException("POSTGRES_HOST is not set - see .env.developer.");
        _port = int.Parse(Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432");
        _username = Environment.GetEnvironmentVariable("POSTGRES_USERNAME");
        _password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

        await DropAndCreateTestDatabaseAsync();

        var testOptions = new PostgresOptions
        {
            Host = _host,
            Port = _port,
            DatabaseName = TestDatabaseName,
            Username = _username,
            Password = _password,
        };

        var contextOptions = new DbContextOptionsBuilder<RowingClubDbContext>()
            .UseNpgsql(testOptions.ConnectionString)
            .Options;

        Context = new RowingClubDbContext(
            contextOptions,
            NoopCurrentTenant.Instance,
            NoopCorrelationIdAccessor.Instance,
            [new PersistenceAssemblyMarker(typeof(RowingClub.Identity.Infrastructure.DependencyInjection).Assembly)]);

        await Context.Database.MigrateAsync();

        UnitOfWork = new EfUnitOfWork(Context);
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();

        // Npgsql pools connections per connection string - clear them first or the DROP DATABASE
        // below fails with "database is being accessed by other users".
        NpgsqlConnection.ClearAllPools();
        await DropTestDatabaseAsync();
    }

    private async Task DropAndCreateTestDatabaseAsync()
    {
        await DropTestDatabaseAsync();

        await using var connection = OpenMaintenanceConnection();
        await connection.OpenAsync();
        await using var create = new NpgsqlCommand($"CREATE DATABASE {TestDatabaseName}", connection);
        await create.ExecuteNonQueryAsync();
    }

    private async Task DropTestDatabaseAsync()
    {
        await using var connection = OpenMaintenanceConnection();
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {TestDatabaseName} WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
    }

    private NpgsqlConnection OpenMaintenanceConnection()
    {
        var maintenanceOptions = new PostgresOptions
        {
            Host = _host,
            Port = _port,
            DatabaseName = "postgres",
            Username = _username,
            Password = _password,
        };

        return new NpgsqlConnection(maintenanceOptions.ConnectionString);
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

[CollectionDefinition(nameof(PostgresIdentityCollection))]
public sealed class PostgresIdentityCollection : ICollectionFixture<PostgresIdentityFixture>;
