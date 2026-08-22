using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Identity.Application.Audit;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.CompanyUsers;

public sealed record CompanyUserDto(
    Guid Id, string Email, string Role, string Status, DateTimeOffset CreatedAtUtc);

/// <summary>
/// Firma içi kullanıcı yönetimi. Yetki denetimi TAMAMEN backend'dedir: çağıran kullanıcının
/// kimliği/firması JWT'den gelir, handler hedefin aynı firmada olduğunu ve çağıranın
/// CompanyAdmin olduğunu her komutta yeniden doğrular - istemciden gelen hiçbir role/companyId
/// değerine güvenilmez.
/// </summary>
public sealed record GetCompanyUsersQuery(Guid CallerUserId) : IRequest<List<CompanyUserDto>>;

public sealed record CreateCompanyUserCommand(
    Guid CallerUserId, string Email, string Password, string Role) : ICommand<CompanyUserDto>;

public sealed record ChangeCompanyUserRoleCommand(
    Guid CallerUserId, Guid TargetUserId, string Role) : ICommand<CompanyUserDto>;

internal static class CompanyUserGuards
{
    /// <summary>Çağıranın aktif bir CompanyAdmin olduğunu ve bir firmaya bağlı olduğunu doğrular.</summary>
    public static async Task<(User Caller, Guid CompanyId)> EnsureCallerIsCompanyAdminAsync(
        IUserRepository userRepository, Guid callerUserId, CancellationToken cancellationToken)
    {
        var caller = await userRepository.GetByIdAsync(callerUserId, cancellationToken)
            ?? throw new DomainException("caller_not_found", "Oturum kullanıcısı bulunamadı.");

        if (caller.Status != UserStatus.Active)
            throw new DomainException("caller_deactivated", "Hesabınız devre dışı.");

        if (caller.Role != UserRole.CompanyAdmin || caller.CompanyId is null)
            throw new DomainException("forbidden", "Bu işlem için firma yöneticisi yetkisi gerekir.");

        return (caller, caller.CompanyId.Value);
    }

    public static UserRole ParseCompanyRole(string raw) => raw.Trim() switch
    {
        // PlatformAdmin bilinçli olarak verilemez: firma yöneticisi yalnızca kendi firması
        // içinde yetki dağıtabilir.
        "CompanyAdmin" => UserRole.CompanyAdmin,
        "Employee" => UserRole.Employee,
        _ => throw new DomainException("invalid_role", "Rol CompanyAdmin veya Employee olmalıdır."),
    };
}

public sealed class GetCompanyUsersQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetCompanyUsersQuery, List<CompanyUserDto>>
{
    public async Task<List<CompanyUserDto>> Handle(
        GetCompanyUsersQuery request, CancellationToken cancellationToken)
    {
        var (_, companyId) = await CompanyUserGuards.EnsureCallerIsCompanyAdminAsync(
            userRepository, request.CallerUserId, cancellationToken);

        var users = await userRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        return users
            .OrderBy(u => u.CreatedAtUtc)
            .Select(u => new CompanyUserDto(
                u.Id, u.Email.Value, u.Role.ToString(), u.Status.ToString(), u.CreatedAtUtc))
            .ToList();
    }
}

public sealed class CreateCompanyUserCommandHandler(
    IUserRepository userRepository,
    ICredentialRepository credentialRepository,
    IPasswordHasher passwordHasher,
    IAuditLogger auditLogger)
    : IRequestHandler<CreateCompanyUserCommand, CompanyUserDto>
{
    public async Task<CompanyUserDto> Handle(
        CreateCompanyUserCommand request, CancellationToken cancellationToken)
    {
        var (caller, companyId) = await CompanyUserGuards.EnsureCallerIsCompanyAdminAsync(
            userRepository, request.CallerUserId, cancellationToken);

        var role = CompanyUserGuards.ParseCompanyRole(request.Role);
        var email = EmailAddress.Create(request.Email);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new DomainException("email_already_registered", "Bu e-posta adresi zaten kayıtlı.");

        var user = User.RegisterCompanyAdmin(email, companyId);
        if (role == UserRole.Employee)
            user.ChangeRole(UserRole.Employee);

        userRepository.Add(user);
        credentialRepository.Add(Credential.Create(user.Id, passwordHasher.Hash(request.Password)));

        auditLogger.Log(
            "COMPANY_USER_CREATED", user.Id.ToString(),
            $"{caller.Email.Value} tarafından {role} rolüyle oluşturuldu.");

        return new CompanyUserDto(user.Id, user.Email.Value, user.Role.ToString(), user.Status.ToString(), user.CreatedAtUtc);
    }
}

public sealed class ChangeCompanyUserRoleCommandHandler(
    IUserRepository userRepository,
    IAuditLogger auditLogger)
    : IRequestHandler<ChangeCompanyUserRoleCommand, CompanyUserDto>
{
    public async Task<CompanyUserDto> Handle(
        ChangeCompanyUserRoleCommand request, CancellationToken cancellationToken)
    {
        var (caller, companyId) = await CompanyUserGuards.EnsureCallerIsCompanyAdminAsync(
            userRepository, request.CallerUserId, cancellationToken);

        var role = CompanyUserGuards.ParseCompanyRole(request.Role);

        var target = await userRepository.GetByIdAsync(request.TargetUserId, cancellationToken)
            ?? throw new DomainException("user_not_found", "Kullanıcı bulunamadı.");

        // Firma sınırı: başka firmanın kullanıcısına ASLA dokunulamaz.
        if (target.CompanyId != companyId)
            throw new DomainException("forbidden", "Bu kullanıcı sizin firmanıza bağlı değil.");

        if (target.Id == caller.Id && role != UserRole.CompanyAdmin)
            throw new DomainException("cannot_demote_self", "Kendi yönetici yetkinizi kaldıramazsınız.");

        target.ChangeRole(role);

        auditLogger.Log(
            "COMPANY_USER_ROLE_CHANGED", target.Id.ToString(),
            $"{caller.Email.Value} tarafından rol {role} yapıldı.");

        return new CompanyUserDto(
            target.Id, target.Email.Value, target.Role.ToString(), target.Status.ToString(), target.CreatedAtUtc);
    }
}
