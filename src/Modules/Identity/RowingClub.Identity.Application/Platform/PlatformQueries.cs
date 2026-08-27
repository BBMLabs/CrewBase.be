using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.Platform;

/// <summary>Master admin: tüm firmalar (durumlarıyla). Varsayılan olarak silinmiş firmalar hariçtir.</summary>
public sealed record GetAllCompaniesQuery(
    bool IncludeDeleted = false,
    string? Search = null,
    string? Status = null,
    string? SortBy = null,
    bool SortDescending = true)
    : IRequest<List<PlatformCompanyDto>>;

public sealed record PlatformCompanyDto(
    Guid Id, string Name, string Subdomain, string Status,
    string? Phone, string? ContactEmail, string? Address, string? TaxNumber,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? LastLoginAtUtc, bool IsDeleted, DateTimeOffset? DeletedAtUtc,
    string Plan, int PlanMaxBranches, int PlanMaxMembers, int PlanMaxBoats);

/// <summary>Master admin: platform geneli sayılar.</summary>
public sealed record GetPlatformStatsQuery : IRequest<PlatformStatsDto>;

public sealed record PlatformStatsDto(
    int TotalCompanies, int ActiveCompanies, int SuspendedCompanies, int RegisteredThisMonth);

/// <summary>
/// Açılışta env'den master admin hesabını garantiler (yoksa oluşturur). Şifre yalnızca env'den
/// gelir ve Argon2id ile saklanır.
/// </summary>
public sealed record EnsurePlatformAdminCommand(string Email, string Password) : ICommand<Unit>;

public sealed class GetAllCompaniesQueryHandler(ICompanyRepository companyRepository, IUserRepository userRepository)
    : IRequestHandler<GetAllCompaniesQuery, List<PlatformCompanyDto>>
{
    public async Task<List<PlatformCompanyDto>> Handle(
        GetAllCompaniesQuery request, CancellationToken cancellationToken)
    {
        var all = new List<Company>();
        foreach (var status in Enum.GetValues<CompanyStatus>())
            all.AddRange(await companyRepository.GetByStatusAsync(status, cancellationToken));

        if (!request.IncludeDeleted)
            all = all.Where(c => !c.IsDeleted).ToList();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            all = all
                .Where(c =>
                    c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    c.Subdomain.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    (c.ContactEmail?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<CompanyStatus>(request.Status, ignoreCase: true, out var statusFilter))
        {
            all = all.Where(c => c.Status == statusFilter).ToList();
        }

        var dtos = new List<PlatformCompanyDto>();
        foreach (var c in all)
        {
            var companyUsers = await userRepository.GetByCompanyIdAsync(c.Id, cancellationToken);
            var admin = companyUsers.FirstOrDefault(u => u.Role == UserRole.CompanyAdmin);

            var limits = c.PlanLimits;
            dtos.Add(new PlatformCompanyDto(
                c.Id, c.Name, c.Subdomain, c.Status.ToString(), c.Phone, c.ContactEmail, c.Address, c.TaxNumber,
                c.CreatedAtUtc, admin?.LastLoginAtUtc, c.IsDeleted, c.DeletedAtUtc,
                c.Plan.ToString(), limits.MaxBranches, limits.MaxMembers, limits.MaxBoats));
        }

        IOrderedEnumerable<PlatformCompanyDto> sorted = request.SortBy?.ToLowerInvariant() switch
        {
            "name" => request.SortDescending ? dtos.OrderByDescending(d => d.Name) : dtos.OrderBy(d => d.Name),
            "status" => request.SortDescending ? dtos.OrderByDescending(d => d.Status) : dtos.OrderBy(d => d.Status),
            "lastlogin" => request.SortDescending
                ? dtos.OrderByDescending(d => d.LastLoginAtUtc)
                : dtos.OrderBy(d => d.LastLoginAtUtc),
            _ => request.SortDescending
                ? dtos.OrderByDescending(d => d.CreatedAtUtc)
                : dtos.OrderBy(d => d.CreatedAtUtc),
        };

        return sorted.ToList();
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
        all = all.Where(c => !c.IsDeleted).ToList();

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
