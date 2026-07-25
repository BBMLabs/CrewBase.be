namespace RowingClub.BuildingBlocks.Application.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid UserId { get; }

    string Email { get; }

    string? Role { get; }

    Guid? CompanyId { get; }

    bool IsPlatformAdmin => Role == "PlatformAdmin";
}
