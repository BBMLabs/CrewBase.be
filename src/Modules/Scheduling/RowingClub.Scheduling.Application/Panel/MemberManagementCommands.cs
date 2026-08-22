using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.Scheduling.Application.Panel;

/// <summary>Firma panelinden üye ekleme (telefonla; üye sonradan aynı telefonla hesap açabilir).</summary>
public sealed record CreateMemberCommand(string FullName, string Phone, string? Email, int Level)
    : ICommand<CustomerDto>;

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
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<CreateMemberCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(CreateMemberCommand request, CancellationToken cancellationToken)
    {
        if (await customerRepository.GetByPhoneAsync(request.Phone, cancellationToken) is not null)
            throw new DomainException("phone_taken", "Bu telefon numarasıyla kayıtlı bir üye zaten var.");

        var customer = Customer.Create(request.FullName, request.Phone, request.Email);
        if (request.Level > 0)
            customer.SetLevel(request.Level);

        customerRepository.Add(customer);
        memberLogRepository.Add(MemberLog.Record(customer.Id, MemberEvents.Registered, "Panelden eklendi"));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CustomerDto(customer.Id, customer.FullName, customer.Phone, customer.Email,
            customer.Level, customer.CreatedAtUtc);
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
