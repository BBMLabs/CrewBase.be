using System.Reflection;

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres;

/// <summary>
/// Each module's Infrastructure project registers one of these (pointing at its own assembly) so
/// <see cref="RowingClubDbContext.OnModelCreating"/> can discover that module's
/// <c>IEntityTypeConfiguration&lt;T&gt;</c> classes via <c>ApplyConfigurationsFromAssembly</c>
/// without BuildingBlocks ever referencing a module directly - the same "modules register
/// themselves into a shared collection" pattern previously used for <c>IMongoMigration</c>.
/// </summary>
public sealed record PersistenceAssemblyMarker(Assembly Assembly);
