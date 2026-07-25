using System.Security.Claims;
using RowingClub.BuildingBlocks.Application.Abstractions;

namespace RowingClub.Api.Security;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId => Guid.TryParse(
        Principal?.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    public string Email => Principal?.FindFirstValue("email") ?? string.Empty;

    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);

    public Guid? CompanyId => Guid.TryParse(
        Principal?.FindFirstValue("company_id"), out var id) ? id : null;
}
