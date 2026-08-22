using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.Platform;

/// <summary>Master admin: tüm firmalar (durumlarıyla).</summary>
public sealed record GetAllCompaniesQuery : IRequest<List<PlatformCompanyDto>>;

public sealed record PlatformCompanyDto(
    Guid Id, string Name, string Subdomain, string Status,
    string? Phone, string? ContactEmail, DateTimeOffset CreatedAtUtc);

/// <summary>Master admin: platform geneli sayılar.</summary>
public sealed record GetPlatformStatsQuery : IRequest<PlatformStatsDto>;

public sealed record PlatformStatsDto(
    int TotalCompanies, int ActiveCompanies, int SuspendedCompanies, int RegisteredThisMonth);

/// <summary>
/// Açılışta env'den master admin hesabını garantiler (yoksa oluşturur). Şifre yalnızca env'den
/// gelir ve Argon2id ile saklanır.
/// </summary>
public sealed record EnsurePlatformAdminCommand(string Email, string Password) : ICommand<Unit>;

public sealed class GetAllCompaniesQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<GetAllCompaniesQuery, List<PlatformCompanyDto>>
{
    public async Task<List<PlatformCompanyDto>> Handle(
        GetAllCompaniesQuery request, CancellationToken cancellationToken)
    {
        var all = new List<Company>();
        foreach (var status in Enum.GetValues<CompanyStatus>())
            all.AddRange(await companyRepository.GetByStatusAsync(status, cancellationToken));

        return all
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new PlatformCompanyDto(
                c.Id, c.Name, c.Subdomain, c.Status.ToString(), c.Phone, c.ContactEmail, c.CreatedAtUtc))
            .ToList();
    }
}

public sealed class GetPlatformStatsQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<GetPlatformStatsQuery, PlatformStatsDto>
{
    public async Task<PlatformStatsDto> Handle(GetPlatformStatsQuery request, CancellationToken cancellationToken)
    {
        var all = new List<Company>();
        foreach (var status in Enum.GetValues<CompanyStatus>())
            all.AddRange(await companyRepository.GetByStatusAsync(status, cancellationToken));

        var now = DateTimeOffset.UtcNow;
        return new PlatformStatsDto(
            all.Count,
            all.Count(c => c.Status == CompanyStatus.Active),
            all.Count(c => c.Status == CompanyStatus.Suspended),
            all.Count(c => c.CreatedAtUtc.Year == now.Year && c.CreatedAtUtc.Month == now.Month));
    }
}

public sealed class EnsurePlatformAdminCommandHandler(
    IUserRepository userRepository,
    ICredentialRepository credentialRepository,
    IPasswordHasher passwordHasher)
    : IRequestHandler<EnsurePlatformAdminCommand, Unit>
{
    public async Task<Unit> Handle(EnsurePlatformAdminCommand request, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(request.Email);
        if (await userRepository.GetByEmailAsync(email, cancellationToken) is not null)
            return Unit.Value;

        var admin = User.Register(email);
        admin.ChangeRole(UserRole.PlatformAdmin);
        admin.VerifyEmail();

        userRepository.Add(admin);
        credentialRepository.Add(Credential.Create(admin.Id, passwordHasher.Hash(request.Password)));

        return Unit.Value;
    }
}
