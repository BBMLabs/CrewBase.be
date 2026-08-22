using MediatR;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.PublicOptions;

/// <summary>Public sitenin dinamik olarak çizilmesi için firmanın kuralları/kaynakları.</summary>
public sealed record GetPublicOptionsQuery : IRequest<PublicOptionsDto>;

public sealed record BoatClassOptionDto(string Value, string Label, int Capacity);

public sealed record PackageOptionDto(Guid Id, string Name, string? Description, int SessionCount, decimal Price);

public sealed record PublicOptionsDto(
    string OpeningTime,
    string ClosingTime,
    int SlotMinutes,
    List<int> OpenDays,
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
    ILessonPackageRepository lessonPackageRepository)
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

        var packages = (await lessonPackageRepository.GetAllAsync(cancellationToken))
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .Select(p => new PackageOptionDto(p.Id, p.Name, p.Description, p.SessionCount, p.Price))
            .ToList();

        return new PublicOptionsDto(
            settings.OpeningTime.ToString("HH:mm"),
            settings.ClosingTime.ToString("HH:mm"),
            settings.SlotMinutes,
            Enumerable.Range(0, 7).Where(d => settings.IsOpenOn((DayOfWeek)d)).ToList(),
            settings.MinNoticeHours,
            settings.MaxAdvanceDays,
            settings.ReminderOptions().ToList(),
            settings.DefaultReminderMinutes,
            activeBoatClasses.Select(c => new BoatClassOptionDto(c.Label(), c.Label(), c.Capacity())).ToList(),
            packages,
            RowingLevels.Labels.ToList());
    }
}
