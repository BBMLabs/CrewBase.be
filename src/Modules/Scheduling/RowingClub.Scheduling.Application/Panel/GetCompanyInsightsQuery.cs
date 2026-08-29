using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;

namespace RowingClub.Scheduling.Application.Panel;

/// <summary>Firma paneli ana ekranı için tekne/üye/gün kırılımlı kullanım istatistikleri.</summary>
public sealed record GetCompanyInsightsQuery : IRequest<CompanyInsightsDto>;

public sealed record BoatUsageDto(Guid BoatId, string BoatName, int AppointmentCount);

public sealed record MemberUsageDto(Guid CustomerId, string FullName, int AppointmentCount);

/// <summary>.NET DayOfWeek değeri (Pazar=0 .. Cumartesi=6) ve Türkçe adı.</summary>
public sealed record WeekdayUsageDto(int DayOfWeek, string DayName, int AppointmentCount);

public sealed record CompanyInsightsDto(
    List<BoatUsageDto> TopBoats,
    List<MemberUsageDto> TopMembers,
    List<WeekdayUsageDto> BusiestWeekdays);

public sealed class GetCompanyInsightsQueryHandler(
    IAppointmentRepository appointmentRepository,
    IBoatRepository boatRepository,
    ITenantDatabase tenantDatabase)
    : IRequestHandler<GetCompanyInsightsQuery, CompanyInsightsDto>
{
    private static readonly string[] WeekdayNamesTr =
        ["Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi"];

    /// <summary>Pazartesi'den başlayan görüntüleme sırası (.NET DayOfWeek: Pazar=0).</summary>
    private static readonly int[] WeekOrder = [1, 2, 3, 4, 5, 6, 0];

    public async Task<CompanyInsightsDto> Handle(GetCompanyInsightsQuery request, CancellationToken cancellationToken)
    {
        if (!tenantDatabase.HasAdvancedReports)
            throw new DomainException("plan_feature_not_available", "Gelişmiş raporlar özelliği mevcut paketinizde yer almıyor.");

        var appointments = await appointmentRepository.GetAllAsync(cancellationToken);
        var boats = await boatRepository.GetAllAsync(cancellationToken);
        var boatNames = boats.ToDictionary(b => b.Id, b => b.Name);

        var active = appointments.Where(a => a.Status != AppointmentStatus.Cancelled).ToList();

        var topBoats = active
            .Where(a => a.Session.BoatId is not null)
            .GroupBy(a => a.Session.BoatId!.Value)
            .Select(g => new BoatUsageDto(
                g.Key,
                boatNames.TryGetValue(g.Key, out var name) ? name : "Bilinmeyen tekne",
                g.Count()))
            .OrderByDescending(b => b.AppointmentCount)
            .Take(5)
            .ToList();

        var topMembers = active
            .GroupBy(a => a.CustomerId)
            .Select(g => new MemberUsageDto(g.Key, g.First().Customer.FullName, g.Count()))
            .OrderByDescending(m => m.AppointmentCount)
            .Take(5)
            .ToList();

        var busiestWeekdays = WeekOrder
            .Select(dow => new WeekdayUsageDto(
                dow, WeekdayNamesTr[dow], active.Count(a => (int)a.Date.DayOfWeek == dow)))
            .ToList();

        return new CompanyInsightsDto(topBoats, topMembers, busiestWeekdays);
    }
}
