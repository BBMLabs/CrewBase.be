using Microsoft.Extensions.Options;
using Npgsql;
using RowingClub.BuildingBlocks.Infrastructure.Postgres;

namespace RowingClub.Scheduling.Infrastructure.Persistence;

/// <summary>
/// Katalog veritabanıyla aynı Postgres sunucusunu/kimlik bilgilerini kullanır; yalnızca veritabanı
/// adı tenant'a göre değişir.
/// </summary>
public sealed class TenantConnectionStringFactory(IOptions<PostgresOptions> postgresOptions)
{
    public string Create(string databaseName)
    {
        EnsureSafeDatabaseName(databaseName);

        var builder = new NpgsqlConnectionStringBuilder(postgresOptions.Value.ConnectionString)
        {
            Database = databaseName,
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// Veritabanı adı SQL kimliği olarak (CREATE DATABASE) ham kullanıldığından yalnızca
    /// SubdomainSlug'ın üretebileceği karakter kümesine izin verilir.
    /// </summary>
    public static void EnsureSafeDatabaseName(string databaseName)
    {
        if (string.IsNullOrEmpty(databaseName) ||
            databaseName.Length > 63 ||
            !databaseName.All(ch => ch is >= 'a' and <= 'z' or >= '0' and <= '9' or '_'))
        {
            throw new InvalidOperationException($"Geçersiz tenant veritabanı adı: '{databaseName}'.");
        }
    }
}
