using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using RowingClub.BuildingBlocks.Application.Abstractions;

namespace RowingClub.Api.Security;

/// <summary>
/// Reads only <c>sub</c> and <c>email</c> off the already-validated JWT - nothing else in the
/// token is treated as authoritative (spec section 11: role/tenant must be re-checked server-side,
/// never trusted from the token itself).
/// </summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId => Guid.TryParse(
        Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : Guid.Empty;

    public string Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;

    public bool IsPlatformAdmin => Principal?.IsInRole("PlatformAdmin") == true;
}
