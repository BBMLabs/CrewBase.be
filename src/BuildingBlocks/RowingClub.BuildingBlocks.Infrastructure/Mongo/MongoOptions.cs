using System.ComponentModel.DataAnnotations;

namespace RowingClub.BuildingBlocks.Infrastructure.Mongo;

/// <summary>Bound from MONGODB_CONNECTION_STRING (spec section 10). The connection string must
/// include <c>replicaSet=rs0</c> (or equivalent) - multi-document transactions used by
/// <see cref="MongoUnitOfWork"/> do not work against a standalone mongod (spec section 8/21).</summary>
public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    [Required] public required string ConnectionString { get; init; }

    [Required] public required string DatabaseName { get; init; }
}
