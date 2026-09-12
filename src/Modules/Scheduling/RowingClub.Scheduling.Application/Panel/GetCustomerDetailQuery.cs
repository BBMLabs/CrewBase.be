using MediatR;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record GetCustomerDetailQuery(Guid CustomerId) : IRequest<CustomerDetailDto?>;

public sealed record CustomerDetailDto(
    Guid Id,
    string MemberCode,
    string FullName,
    string Phone,
    string? Email,
    int Level,
    string LevelLabel,
    bool IsActive,
    Guid? BranchId,
    string? BranchName,
    bool EmailVerified,
    bool PhoneVerified,
    DateTimeOffset CreatedAtUtc,
    List<CustomerDetailActivePackageDto> ActivePackages,
    List<CustomerDetailAppointmentDto> UpcomingAppointments);

public sealed record CustomerDetailActivePackageDto(
    Guid Id, string PackageName, int TotalLessons, int UsedLessons, int RemainingLessons,
    DateTimeOffset? ExpiresAtUtc, DateTimeOffset AssignedAtUtc, string Source);

public sealed record CustomerDetailAppointmentDto(
    Guid Id, Guid? MemberId, string MemberName, string? MemberPhone, Guid SessionId,
    string Date, string StartTime, string BoatClass, string? BoatName, string? InstructorName,
    string Status, bool UsePackage, DateTimeOffset CreatedAtUtc);

public sealed class GetCustomerDetailQueryHandler(
    ICustomerRepository customerRepository,
    ICustomerPackageRepository customerPackageRepository,
    IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetCustomerDetailQuery, CustomerDetailDto?>
{
    private static readonly string[] LevelLabels = ["Başlangıç", "Temel", "Orta", "İleri", "Usta"];

    public async Task<CustomerDetailDto?> Handle(GetCustomerDetailQuery request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        if (customer is null) return null;

        var packages = await customerPackageRepository.GetByCustomerAsync(customer.Id, cancellationToken);
        var activePackages = packages
            .OrderByDescending(p => p.AssignedAtUtc)
            .Select(p => new CustomerDetailActivePackageDto(
                p.Id, p.PackageName, p.TotalSessions,
                p.TotalSessions - p.RemainingSessions, p.RemainingSessions,
                p.ExpiresAtUtc, p.AssignedAtUtc, p.Source.ToString()))
            .ToList();

        var allAppointments = await appointmentRepository.GetAllAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcoming = allAppointments
            .Where(a => a.CustomerId == customer.Id
                && a.Status != AppointmentStatus.Cancelled
                && a.Date >= today)
            .OrderBy(a => a.Date).ThenBy(a => a.StartTime)
            .Take(10)
            .Select(a => new CustomerDetailAppointmentDto(
                a.Id, a.CustomerId, a.Customer.FullName, a.Customer.Phone, a.SessionId,
                a.Date.ToString("yyyy-MM-dd"), a.StartTime.ToString("HH:mm"),
                a.Session.BoatClass.Label(), a.Session.Boat?.Name,
                a.Session.Instructor?.FullName, a.Status.ToString(),
                false, a.CreatedAtUtc))
            .ToList();

        var levelLabel = customer.Level >= 1 && customer.Level <= LevelLabels.Length
            ? LevelLabels[customer.Level - 1] : $"Seviye {customer.Level}";

        return new CustomerDetailDto(
            customer.Id,
            customer.MemberCode ?? "",
            customer.FullName,
            customer.Phone,
            customer.Email,
            customer.Level,
            levelLabel,
            !customer.IsBlocked,
            customer.BranchId,
            null,
            customer.Email is not null,
            false,
            customer.CreatedAtUtc,
            activePackages,
            upcoming);
    }
}
