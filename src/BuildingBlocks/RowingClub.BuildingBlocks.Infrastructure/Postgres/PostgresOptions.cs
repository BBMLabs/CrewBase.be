using System.ComponentModel.DataAnnotations;

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres;

/// <summary>
/// Bound from POSTGRES_HOST/POSTGRES_PORT/POSTGRES_DATABASE_NAME/POSTGRES_USERNAME/POSTGRES_PASSWORD.
/// The backend builds the actual Npgsql connection string from these pieces
/// (<see cref="ConnectionString"/>) rather than taking a pre-assembled one from configuration,
/// mirroring MongoOptions/RedisOptions.
/// </summary>
public sealed class PostgresOptions
{
    public const string SectionName = "Postgres";

    [Required] public required string Host { get; init; }

    [Range(1, 65535)] public required int Port { get; init; }

    [Required] public required string DatabaseName { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

    public string ConnectionString
    {
        get
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder
            {
                Host = Host,
                Port = Port,
                Database = DatabaseName,
            };

            if (Username is not null && Password is not null)
            {
                builder.Username = Username;
                builder.Password = Password;
            }

            return builder.ConnectionString;
        }
    }
}
