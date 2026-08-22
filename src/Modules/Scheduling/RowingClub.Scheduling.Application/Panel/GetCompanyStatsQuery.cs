using MediatR;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Sessions;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.Panel;

/// <summary>Firma paneli ana ekran istatistikleri.</summary>
public sealed record GetCompanyStatsQuery : IRequest<CompanyStatsDto>;

public sealed record CompanyStatsDto(
    int MemberCount,
    int MembersWithAccount,
    int AppointmentsToday,
    int AppointmentsThisMonth,
    Dictionary<string, int> ThisMonthByStatus,
    int SessionsToday,
    int ActivePackageBalances,
    int TotalRemainingSessions,
    List<DailyCountDto> Next7Days);

public sealed record DailyCountDto(DateOnly Date, int Appointments);

public sealed class GetCompanyStatsQueryHandler(
    ICustomerRepository customerRepository,
    IAppointmentRepository appointmentRepository,
    ITrainingSessionRepository sessionRepository,
    ICustomerPackageRepository customerPackageRepository,
    ISettingsRepository settingsRepository)
    : IRequestHandler<GetCompanyStatsQuery, CompanyStatsDto>
{
    public async Task<CompanyStatsDto> Handle(GetCompanyStatsQuery request, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken) ?? CompanySettings.Default();
        var today = DateOnly.FromDateTime(settings.NowLocal());

        var customers = await customerRepository.GetAllAsync(cancellationToken);
        var appointments = await appointmentRepository.GetAllAsync(cancellationToken);
        var todaySessions = await sessionRepository.GetByDateAsync(today, cancellationToken);
        var packages = await customerPackageRepository.GetAllAsync(cancellationToken);

        var monthAppointments = appointments
            .Where(a => a.Date.Year == today.Year && a.Date.Month == today.Month)
            .ToList();

        var next7 = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var date = today.AddDays(offset);
                return new DailyCountDto(date, appointments.Count(
                    a => a.Date == date && a.Status != AppointmentStatus.Cancelled));
            })
            .ToList();

        return new CompanyStatsDto(
            customers.Count,
            customers.Count(c => c.HasAccount),
            appointments.Count(a => a.Date == today && a.Status != AppointmentStatus.Cancelled),
            monthAppointments.Count,
            monthAppointments.GroupBy(a => a.Status.ToString()).ToDictionary(g => g.Key, g => g.Count()),
            todaySessions.Count,
            packages.Count(p => p.RemainingSessions > 0),
            packages.Sum(p => p.RemainingSessions),
            next7);
    }
}
