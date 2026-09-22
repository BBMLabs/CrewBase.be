namespace RowingClub.BuildingBlocks.Application.Abstractions;

public interface ITenantDatabaseProvisioner
{
    Task ProvisionAsync(string databaseName, CancellationToken cancellationToken);

    Task DeprovisionAsync(string databaseName, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListTenantDatabaseNamesAsync(CancellationToken cancellationToken);

    Task<Dictionary<string, long>> GetDatabaseSizesAsync(CancellationToken cancellationToken);

    Task<TenantDatabaseUsageStats> GetUsageStatsAsync(string databaseName, CancellationToken cancellationToken);
}

public sealed record TenantDatabaseUsageStats(int? MemberCount, int? AppointmentCount, bool Available)
{
    public static TenantDatabaseUsageStats Unavailable { get; } = new(null, null, false);
}
