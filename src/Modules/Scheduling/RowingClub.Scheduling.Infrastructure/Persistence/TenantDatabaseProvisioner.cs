using Microsoft.EntityFrameworkCore;
using Npgsql;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Security.Encryption;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Infrastructure.Persistence;

public sealed class TenantDatabaseProvisioner(
    NpgsqlDataSource catalogDataSource,
    TenantConnectionStringFactory connectionStringFactory,
    IFieldEncryptor fieldEncryptor,
    IBlindIndexer blindIndexer)
    : ITenantDatabaseProvisioner
{
    public async Task ProvisionAsync(string databaseName, CancellationToken cancellationToken)
    {
        TenantConnectionStringFactory.EnsureSafeDatabaseName(databaseName);

        await using (var connection = await catalogDataSource.OpenConnectionAsync(cancellationToken))
        {
            await using var existsCommand = new NpgsqlCommand(
                "SELECT 1 FROM pg_database WHERE datname = @name", connection);
            existsCommand.Parameters.AddWithValue("name", databaseName);

            var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;
            if (!exists)
            {
                await using var createCommand = new NpgsqlCommand(
                    $"CREATE DATABASE \"{databaseName}\"", connection);
                await createCommand.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionStringFactory.Create(databaseName))
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var tenantContext = new TenantDbContext(options, fieldEncryptor, blindIndexer);
        await tenantContext.Database.MigrateAsync(cancellationToken);

        if (!await tenantContext.Settings.AnyAsync(cancellationToken))
        {
            tenantContext.Settings.Add(CompanySettings.Default());
            await tenantContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeprovisionAsync(string databaseName, CancellationToken cancellationToken)
    {
        TenantConnectionStringFactory.EnsureSafeDatabaseName(databaseName);

        await using var connection = await catalogDataSource.OpenConnectionAsync(cancellationToken);
        await using var dropCommand = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", connection);
        await dropCommand.ExecuteNonQueryAsync(cancellationToken);

        NpgsqlConnection.ClearAllPools();
    }

    public async Task<IReadOnlyList<string>> ListTenantDatabaseNamesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await catalogDataSource.OpenConnectionAsync(cancellationToken);
        await using var listCommand = new NpgsqlCommand(
            "SELECT datname FROM pg_database WHERE datname ~ '^tenant_' ORDER BY datname", connection);

        var names = new List<string>();
        await using var reader = await listCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            names.Add(reader.GetString(0));

        return names;
    }

    public async Task<Dictionary<string, long>> GetDatabaseSizesAsync(CancellationToken cancellationToken)
    {
        await using var connection = await catalogDataSource.OpenConnectionAsync(cancellationToken);
        await using var sizeCommand = new NpgsqlCommand(
            "SELECT datname, pg_database_size(datname) FROM pg_database WHERE datname ~ '^tenant_'", connection);

        var sizes = new Dictionary<string, long>();
        await using var reader = await sizeCommand.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            sizes[reader.GetString(0)] = reader.GetInt64(1);

        return sizes;
    }

    public async Task<TenantDatabaseUsageStats> GetUsageStatsAsync(string databaseName, CancellationToken cancellationToken)
    {
        try
        {
            TenantConnectionStringFactory.EnsureSafeDatabaseName(databaseName);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var builder = new NpgsqlConnectionStringBuilder(connectionStringFactory.Create(databaseName))
            {
                Timeout = 5,
                CommandTimeout = 5,
            };

            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(linkedCts.Token);

            var memberCount = await CountRowsAsync(connection, "customers", linkedCts.Token);
            var appointmentCount = await CountRowsAsync(connection, "appointments", linkedCts.Token);

            return new TenantDatabaseUsageStats(memberCount, appointmentCount, Available: true);
        }
        catch
        {
            return TenantDatabaseUsageStats.Unavailable;
        }
    }

    private static async Task<int> CountRowsAsync(NpgsqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        await using var countCommand = new NpgsqlCommand($"SELECT COUNT(*) FROM {tableName}", connection);
        var result = await countCommand.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }
}
