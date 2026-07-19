using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace RowingClub.BuildingBlocks.Infrastructure.Mongo;

/// <summary>
/// Process-wide BSON serialization setup, applied once regardless of how many module DbContext-
/// equivalents get constructed. <see cref="GuidSerializer"/> must be registered explicitly or the
/// driver refuses to (de)serialize any <c>Guid</c> - every aggregate id in this codebase is a Guid.
/// </summary>
public static class MongoBsonConfiguration
{
    private static int _configured;

    public static void EnsureConfigured()
    {
        if (Interlocked.Exchange(ref _configured, 1) == 1)
        {
            return;
        }

        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        ConventionRegistry.Register(
            "RowingClub",
            new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String),
            },
            _ => true);
    }
}
