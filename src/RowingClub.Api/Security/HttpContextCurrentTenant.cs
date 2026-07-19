using RowingClub.BuildingBlocks.Application.Abstractions;

namespace RowingClub.Api.Security;

/// <summary>
/// Resolves the active club purely from the <c>X-Club-Id</c> header for now. Cross-checking it
/// against the caller's actual active memberships (spec section 7, kural 2) happens once the
/// Memberships module exists - Identity itself has no notion of club membership, so there is
/// nothing to validate against yet. Every module that DOES need real tenant isolation must not
/// ship without that check wired in here first.
/// </summary>
public sealed class HttpContextCurrentTenant(IHttpContextAccessor httpContextAccessor) : ICurrentTenant
{
    public const string HeaderName = "X-Club-Id";

    public bool IsSet => TryGetClubId(out _);

    public Guid ClubId => TryGetClubId(out var clubId) ? clubId : throw new InvalidOperationException(
        $"Aktif kulüp bağlamı yok. Endpoint çağrılmadan önce {HeaderName} header'ı doğrulanmalıdır.");

    private bool TryGetClubId(out Guid clubId)
    {
        clubId = Guid.Empty;

        var header = httpContextAccessor.HttpContext?.Request.Headers[HeaderName].ToString();
        return !string.IsNullOrWhiteSpace(header) && Guid.TryParse(header, out clubId);
    }
}
