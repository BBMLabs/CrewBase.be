namespace RowingClub.BuildingBlocks.Application.Abstractions;

/// <summary>
/// The active club (tenant) for the current request, resolved and server-side validated by API
/// middleware from the <c>X-Club-Id</c> header against the caller's active memberships
/// (spec section 7). Application/Infrastructure code must never accept a club id from a DTO or
/// route parameter as authoritative - only this abstraction.
/// </summary>
public interface ICurrentTenant
{
    bool IsSet { get; }

    Guid ClubId { get; }
}
