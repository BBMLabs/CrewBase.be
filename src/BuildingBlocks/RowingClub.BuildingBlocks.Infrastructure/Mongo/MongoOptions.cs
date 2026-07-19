using System.ComponentModel.DataAnnotations;

namespace RowingClub.BuildingBlocks.Infrastructure.Mongo;

/// <summary>
/// Bound from MONGODB_HOST/MONGODB_PORT/MONGODB_DATABASE_NAME/MONGODB_USERNAME/MONGODB_PASSWORD
/// (spec section 10). The backend builds the actual connection string from these pieces
/// (<see cref="ConnectionString"/>) rather than taking a pre-assembled one from configuration -
/// <c>replicaSet=rs0</c> is always appended since <see cref="MongoUnitOfWork"/>'s multi-document
/// transactions do not work against a standalone mongod (spec section 8/21). Username/password are
/// optional - a local dev replica set typically runs without auth.
/// </summary>
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    [Required] public required string Host { get; init; }

    [Range(1, 65535)] public required int Port { get; init; }

    [Required] public required string DatabaseName { get; init; }

    public string? Username { get; init; }

    public string? Password { get; init; }

    public string ConnectionString
    {
        get
        {
            var hasCredentials = Username is not null && Password is not null;

            var credentials = hasCredentials
                ? $"{Uri.EscapeDataString(Username!)}:{Uri.EscapeDataString(Password!)}@"
                : string.Empty;

            // Root/admin users are created against the "admin" database - without authSource=admin
            // the driver defaults to authenticating against DatabaseName, where that user doesn't
            // exist, and auth fails.
            var authSource = hasCredentials ? "&authSource=admin" : string.Empty;

            return $"mongodb://{credentials}{Host}:{Port}/{DatabaseName}?replicaSet=rs0{authSource}";
        }
    }
}
