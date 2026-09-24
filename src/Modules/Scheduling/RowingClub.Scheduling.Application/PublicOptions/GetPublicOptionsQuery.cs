using MediatR;
using RowingClub.Scheduling.Application.Panel;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Campaigns;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.PublicOptions;

/// <summary>Public sitenin dinamik olarak çizilmesi için firmanın kuralları/kaynakları.</summary>
public sealed record GetPublicOptionsQuery : IRequest<PublicOptionsDto>;

public sealed record BoatClassOptionDto(string Value, string Label, int Capacity);

public sealed record PackageOptionDto(Guid Id, string Name, string? Description, int SessionCount, decimal Price);

public sealed record PublicOptionsDto(
    List<DayScheduleDto> WorkingHours,
    int SlotMinutes,
    int MinNoticeHours,
    int MaxAdvanceDays,
    List<int> ReminderOptions,
    int DefaultReminderMinutes,
    List<BoatClassOptionDto> BoatClasses,
    List<PackageOptionDto> Packages,
    List<string> LevelLabels);

public sealed class GetPublicOptionsQueryHandler(
    ISettingsRepository settingsRepository,
    IBoatRepository boatRepository,
    ILessonPackageRepository lessonPackageRepository,
    ICampaignRepository campaignRepository)
    : IRequestHandler<GetPublicOptionsQuery, PublicOptionsDto>
{
    public async Task<PublicOptionsDto> Handle(GetPublicOptionsQuery request, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken) ?? CompanySettings.Default();

        var activeBoatClasses = (await boatRepository.GetAllAsync(cancellationToken))
            .Where(b => b.IsActive)
            .Select(b => b.Class)
            .Distinct()
            .OrderBy(c => (int)c)
            .ToList();

        // Hiç tekne tanımlı değilse bireysel (1x) rezervasyon her zaman mümkündür.
        if (activeBoatClasses.Count == 0)
            activeBoatClasses.Add(BoatClass.Single1x);

        var now = DateTimeOffset.UtcNow;
        var campaigns = await campaignRepository.GetAllAsync(cancellationToken);
        // Ziyaretçi henüz üye değil (seviyesi yok) - yalnızca TÜM üyelere açık (seviye kısıtı
        // olmayan) kampanyalar herkese güvenle gösterilebilir.
        var publicCampaigns = campaigns.Where(c => c.MinLevel is null && c.MaxLevel is null).ToList();

        decimal EffectivePrice(LessonPackage p) =>
            publicCampaigns.FirstOrDefault(c => c.LessonPackageId == p.Id && c.IsActiveAt(now))?.Price ?? p.Price;

        var packages = (await lessonPackageRepository.GetAllAsync(cancellationToken))
            .Where(p => p.IsActive)
            .OrderBy(EffectivePrice)
            .Select(p => new PackageOptionDto(p.Id, p.Name, p.Description, p.SessionCount, EffectivePrice(p)))
            .ToList();

        return new PublicOptionsDto(
            settings.DaySchedules
                .OrderBy(d => (int)d.Day)
                .Select(d => new DayScheduleDto(
                    (int)d.Day, d.IsOpen, d.OpeningTime.ToString("HH:mm"), d.ClosingTime.ToString("HH:mm")))
                .ToList(),
            settings.SlotMinutes,
            settings.EffectiveNoticeHours,
            settings.MaxAdvanceDays,
            settings.ReminderOptions().ToList(),
            settings.DefaultReminderMinutes,
            activeBoatClasses.Select(c => new BoatClassOptionDto(c.Label(), c.Label(), c.Capacity())).ToList(),
            packages,
            RowingLevels.Labels.ToList());
    }
}
