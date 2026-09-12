using System.Security.Claims;
using RowingClub.BuildingBlocks.Security.Jwt;
using RowingClub.Identity.Application.Companies.GetCompanySite;
using RowingClub.Scheduling.Application.Members;

namespace RowingClub.Api.Tenancy;

/// <summary>
/// Üye (Member) erişim token'ı üretir: sub = tenant DB'deki CustomerId, rol = Member,
/// company_id = firma. Firma kullanıcılarının refresh-token altyapısından bilinçli olarak ayrıdır;
/// üye oturumu kısa ömürlü access token ile yürür, süresi dolunca yeniden giriş yapılır.
/// </summary>
public sealed class MemberTokenIssuer(IJwtTokenService jwtTokenService)
{
    public (string Token, DateTimeOffset ExpiresAtUtc) Issue(MemberDto member, CompanySiteDto company)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "Member"),
            new("company_id", company.CompanyId.ToString()),
            new("subdomain", company.Subdomain),
        };

        var issued = jwtTokenService.IssueAccessToken(member.CustomerId, member.Email ?? string.Empty, claims);
        return (issued.Token, issued.ExpiresAtUtc);
    }
}
