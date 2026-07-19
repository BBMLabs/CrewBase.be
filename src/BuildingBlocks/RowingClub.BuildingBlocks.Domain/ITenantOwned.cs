namespace RowingClub.BuildingBlocks.Domain;

/// <summary>
/// Implemented by every aggregate whose data must be isolated per club (tenant). EF Core applies
/// a global query filter on <see cref="ClubId"/> for all types implementing this interface, and
/// <c>SaveChanges</c> validates it was not tampered with (spec section 7, kural 4 ve 5).
/// </summary>
public interface ITenantOwned
{
    Guid ClubId { get; }
}
