using MediatR;
using Microsoft.Extensions.Logging;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.Scheduling.Application.Panel;

/// <summary>
/// Firma panelinden üye ekleme (telefonla; üye sonradan aynı telefonla hesap açabilir). E-posta
/// verilirse üyeye kendi şifresini oluşturması için tek kullanımlık bir bağlantı e-postayla
/// gönderilir - şifre panelden ASLA üretilip iletilmez, üye kendi belirler. CompanyName/Subdomain
/// yalnızca bu e-postanın içeriği/bağlantısı için taşınır, başka bir amaçla kullanılmaz.
/// </summary>
public sealed record CreateMemberCommand(
    string FullName, string Phone, string? Email, int Level, Guid? BranchId,
    string CompanyName, string Subdomain) : ICommand<CustomerDto>;

/// <summary>Var olan bir üyeyi bir şubeye atar/şubesini kaldırır (BranchId null = şubesiz).</summary>
public sealed record SetCustomerBranchCommand(Guid CustomerId, Guid? BranchId) : ICommand<CustomerDto>;

/// <summary>Üyeye ders paketi tanımlama; bakiye takibi CustomerPackage üzerinden yürür.</summary>
public sealed record AssignPackageCommand(Guid CustomerId, Guid LessonPackageId)
    : ICommand<CustomerPackageDto>;

/// <summary>Tüm üyelerin paket bakiyeleri (CustomerId verilirse tek üye) - düşüm takibi ekranı.</summary>
public sealed record GetCustomerPackagesQuery(Guid? CustomerId) : IRequest<List<CustomerPackageBalanceDto>>;

public sealed record CustomerPackageBalanceDto(
    Guid Id, Guid CustomerId, string CustomerName, string PackageName,
    int TotalSessions, int RemainingSessions, int UsedSessions, DateTimeOffset AssignedAtUtc);

/// <summary>Üye hareket logları (giriş, randevu, paket düşümü...).</summary>
public sealed record GetMemberLogsQuery(Guid? CustomerId, int Take) : IRequest<List<MemberLogDto>>;

public sealed record MemberLogDto(Guid CustomerId, string CustomerName, string Event, string? Details, DateTimeOffset AtUtc);

public sealed class CreateMemberCommandHandler(
    ICustomerRepository customerRepository,
    IMemberLogRepository memberLogRepository,
    IMemberPasswordSetupTokenRepository setupTokenRepository,
    IOpaqueTokenGenerator tokenGenerator,
    IRefreshTokenHasher tokenHasher,
    IMemberWelcomeEmailSender welcomeEmailSender,
    ISchedulingUnitOfWork unitOfWork,
    ITenantDatabase tenantDatabase,
    ILogger<CreateMemberCommandHandler> logger)
    : IRequestHandler<CreateMemberCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateMemberCommand request, CancellationToken cancellationToken)
    {
        if (await customerRepository.GetByPhoneAsync(request.Phone, cancellationToken) is not null)
            throw new DomainException("phone_taken", "Bu telefon numarasıyla kayıtlı bir üye zaten var.");

        if (await customerRepository.CountAsync(cancellationToken) >= tenantDatabase.MaxMembers)
            throw new DomainException("member_limit_reached",
                $"Üye limitine ulaşıldı. Mevcut paketiniz en fazla {tenantDatabase.MaxMembers} üyeye izin verir; devam etmek için paketinizi yükseltin.");

        var customer = Customer.Create(request.FullName, request.Phone, request.Email, request.BranchId);
        if (request.Level > 0)
            customer.SetLevel(request.Level);

        string? rawSetupToken = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            rawSetupToken = tokenGenerator.Generate();
            setupTokenRepository.Add(
                MemberPasswordSetupToken.Issue(customer.Id, tokenHasher.Hash(rawSetupToken), TimeSpan.FromDays(7)));
        }

        customerRepository.Add(customer);
        memberLogRepository.Add(MemberLog.Record(customer.Id, MemberEvents.Registered, "Panelden eklendi"));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (rawSetupToken is not null)
        {
            try
            {
                await welcomeEmailSender.SendAsync(
                    customer.Email!, customer.FullName, request.CompanyName, rawSetupToken,
                    request.Subdomain, cancellationToken);
            }
            catch (Exception ex)
            {
                // Üye zaten oluşturuldu; e-posta gönderimi başarısız olsa da işlem geri alınmaz
                // (SMTP geçici olarak erişilemez olabilir) - yalnızca loglanır.
                logger.LogError(ex, "Üye hoş geldin e-postası gönderilemedi: {CustomerId}", customer.Id);
            }
        }

        return new CustomerDto(customer.Id, customer.FullName, customer.Phone, customer.Email,
            customer.Level, customer.BranchId, customer.IsBlocked, customer.CreatedAtUtc);
    }
}

public sealed class SetCustomerBranchCommandHandler(
    ICustomerRepository customerRepository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SetCustomerBranchCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(SetCustomerBranchCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        customer.SetBranch(request.BranchId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CustomerDto(customer.Id, customer.FullName, customer.Phone, customer.Email,
            customer.Level, customer.BranchId, customer.IsBlocked, customer.CreatedAtUtc);
    }
}

/// <summary>Firma panelinden üyeye şifre sıfırlama e-postası gönderir (yeni tek kullanımlık bağlantı).</summary>
public sealed record SendMemberPasswordResetCommand(
    Guid CustomerId, string CompanyName, string Subdomain) : ICommand<CustomerDto>;

public sealed class SendMemberPasswordResetCommandHandler(
    ICustomerRepository customerRepository,
    IMemberPasswordSetupTokenRepository setupTokenRepository,
    IOpaqueTokenGenerator tokenGenerator,
    IRefreshTokenHasher tokenHasher,
    IMemberWelcomeEmailSender welcomeEmailSender,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SendMemberPasswordResetCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(SendMemberPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        if (string.IsNullOrWhiteSpace(customer.Email))
            throw new DomainException("member_no_email", "Bu üyenin kayıtlı bir e-posta adresi yok.");

        var rawToken = tokenGenerator.Generate();
        setupTokenRepository.Add(
            MemberPasswordSetupToken.Issue(customer.Id, tokenHasher.Hash(rawToken), TimeSpan.FromHours(1)));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await welcomeEmailSender.SendPasswordResetAsync(
            customer.Email, customer.FullName, request.CompanyName, rawToken, request.Subdomain, cancellationToken);

        return new CustomerDto(customer.Id, customer.FullName, customer.Phone, customer.Email,
            customer.Level, customer.BranchId, customer.IsBlocked, customer.CreatedAtUtc);
    }
}

/// <summary>Üyeyi üye paneli girişinden men eder; randevu/telefon kaydı etkilenmez.</summary>
public sealed record BlockMemberCommand(Guid CustomerId) : ICommand<CustomerDto>;

/// <summary>Daha önce engellenmiş bir üyenin giriş erişimini geri verir.</summary>
public sealed record UnblockMemberCommand(Guid CustomerId) : ICommand<CustomerDto>;

public sealed class BlockMemberCommandHandler(ICustomerRepository customerRepository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<BlockMemberCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(BlockMemberCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        customer.Block();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CustomerDto(customer.Id, customer.FullName, customer.Phone, customer.Email,
            customer.Level, customer.BranchId, customer.IsBlocked, customer.CreatedAtUtc);
    }
}

public sealed class UnblockMemberCommandHandler(ICustomerRepository customerRepository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<UnblockMemberCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(UnblockMemberCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        customer.Unblock();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CustomerDto(customer.Id, customer.FullName, customer.Phone, customer.Email,
            customer.Level, customer.BranchId, customer.IsBlocked, customer.CreatedAtUtc);
    }
}

public sealed class AssignPackageCommandHandler(
    ICustomerRepository customerRepository,
    ILessonPackageRepository lessonPackageRepository,
    ICustomerPackageRepository customerPackageRepository,
    IMemberLogRepository memberLogRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<AssignPackageCommand, CustomerPackageDto>
{
    public async Task<CustomerPackageDto> Handle(AssignPackageCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        var package = await lessonPackageRepository.GetByIdAsync(request.LessonPackageId, cancellationToken);
        if (package is null || !package.IsActive)
            throw new DomainException("invalid_package", "Paket bulunamadı veya aktif değil.");

        var assigned = CustomerPackage.Assign(customer.Id, package);
        customerPackageRepository.Add(assigned);
        memberLogRepository.Add(MemberLog.Record(
            customer.Id, MemberEvents.PackageAssigned, $"{package.Name} ({package.SessionCount} ders)"));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CustomerPackageDto(
            assigned.Id, assigned.CustomerId, assigned.PackageName,
            assigned.TotalSessions, assigned.RemainingSessions, assigned.AssignedAtUtc);
    }
}

public sealed class GetCustomerPackagesQueryHandler(
    ICustomerPackageRepository customerPackageRepository,
    ICustomerRepository customerRepository)
    : IRequestHandler<GetCustomerPackagesQuery, List<CustomerPackageBalanceDto>>
{
    public async Task<List<CustomerPackageBalanceDto>> Handle(
        GetCustomerPackagesQuery request, CancellationToken cancellationToken)
    {
        var packages = request.CustomerId is { } customerId
            ? await customerPackageRepository.GetByCustomerAsync(customerId, cancellationToken)
            : await customerPackageRepository.GetAllAsync(cancellationToken);

        var customers = (await customerRepository.GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Id, c => c.FullName);

        return packages
            .OrderByDescending(p => p.AssignedAtUtc)
            .Select(p => new CustomerPackageBalanceDto(
                p.Id, p.CustomerId, customers.GetValueOrDefault(p.CustomerId, "-"), p.PackageName,
                p.TotalSessions, p.RemainingSessions, p.TotalSessions - p.RemainingSessions, p.AssignedAtUtc))
            .ToList();
    }
}

public sealed class GetMemberLogsQueryHandler(
    IMemberLogRepository memberLogRepository,
    ICustomerRepository customerRepository)
    : IRequestHandler<GetMemberLogsQuery, List<MemberLogDto>>
{
    public async Task<List<MemberLogDto>> Handle(GetMemberLogsQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 500);
        var logs = request.CustomerId is { } customerId
            ? await memberLogRepository.GetByCustomerAsync(customerId, take, cancellationToken)
            : await memberLogRepository.GetRecentAsync(take, cancellationToken);

        var customers = (await customerRepository.GetAllAsync(cancellationToken))
            .ToDictionary(c => c.Id, c => c.FullName);

        return logs
            .Select(l => new MemberLogDto(
                l.CustomerId, customers.GetValueOrDefault(l.CustomerId, "(silinmiş üye)"),
                l.Event, l.Details, l.AtUtc))
            .ToList();
    }
}
