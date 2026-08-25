using RowingClub.BuildingBlocks.Domain;

namespace RowingClub.Scheduling.Domain.Boats;

public enum BoatClass
{
    Single1x = 1,
    Double2x = 2,
    Quad4x = 4,
}

public static class BoatClassExtensions
{
    /// <summary>Teknedeki koltuk sayısı: 1x=1 (tek), 2x=2, 4x=4 kişilik.</summary>
    public static int Capacity(this BoatClass boatClass) => boatClass switch
    {
        BoatClass.Single1x => 1,
        BoatClass.Double2x => 2,
        BoatClass.Quad4x => 4,
        _ => 1,
    };

    public static string Label(this BoatClass boatClass) => boatClass switch
    {
        BoatClass.Single1x => "1x",
        BoatClass.Double2x => "2x",
        BoatClass.Quad4x => "4x",
        _ => boatClass.ToString(),
    };

    public static BoatClass Parse(string raw) => raw.Trim().ToLowerInvariant() switch
    {
        "1x" or "single1x" or "1" => BoatClass.Single1x,
        "2x" or "double2x" or "2" => BoatClass.Double2x,
        "4x" or "quad4x" or "4" => BoatClass.Quad4x,
        _ => throw new DomainException("invalid_boat_class", "Tekne sınıfı 1x, 2x veya 4x olmalıdır."),
    };
}

public sealed class Boat
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public BoatClass Class { get; private set; }

    public bool IsActive { get; private set; }

    public Guid? BranchId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Boat()
    {
    }

    public static Boat Create(string name, BoatClass boatClass, Guid? branchId = null) => new()
    {
        Id = Guid.NewGuid(),
        Name = name.Trim(),
        Class = boatClass,
        IsActive = true,
        BranchId = branchId,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    public void Update(string name, BoatClass boatClass, bool isActive, Guid? branchId = null)
    {
        Name = name.Trim();
        Class = boatClass;
        IsActive = isActive;
        BranchId = branchId;
    }
}
