using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record CompanySettingsDto(
    string OpeningTime,
    string ClosingTime,
    int SlotMinutes,
    List<int> OpenDays,
    int MinNoticeHours,
    int MaxAdvanceDays,
    List<int> ReminderOptions,
    int DefaultReminderMinutes,
    string TimeZoneId);

public sealed record GetSettingsQuery : IRequest<CompanySettingsDto>;

/// <summary>Firma çalışma düzenini ve randevu/hatırlatma kurallarını günceller (tamamı dinamik).</summary>
public sealed record UpdateSettingsCommand(
    string OpeningTime,
    string ClosingTime,
    int SlotMinutes,
    List<int> OpenDays,
    int MinNoticeHours,
    int MaxAdvanceDays,
    List<int> ReminderOptions,
    int DefaultReminderMinutes,
    string TimeZoneId) : ICommand<CompanySettingsDto>;

public sealed class GetSettingsQueryHandler(ISettingsRepository settingsRepository)
    : IRequestHandler<GetSettingsQuery, CompanySettingsDto>
{
    public async Task<CompanySettingsDto> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken) ?? CompanySettings.Default();
        return SettingsMapper.ToDto(settings);
    }
}

public sealed class UpdateSettingsCommandHandler(
    ISettingsRepository settingsRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<UpdateSettingsCommand, CompanySettingsDto>
{
    public async Task<CompanySettingsDto> Handle(UpdateSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await settingsRepository.GetAsync(cancellationToken);
        if (settings is null)
        {
            settings = CompanySettings.Default();
            settingsRepository.Add(settings);
        }

        var openDaysMask = request.OpenDays.Aggregate(0, (mask, day) => mask | (1 << day));

        settings.Update(
            ParseTime(request.OpeningTime), ParseTime(request.ClosingTime), request.SlotMinutes,
            openDaysMask, request.MinNoticeHours, request.MaxAdvanceDays,
            request.ReminderOptions, request.DefaultReminderMinutes, request.TimeZoneId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return SettingsMapper.ToDto(settings);
    }

    private static TimeOnly ParseTime(string raw) =>
        TimeOnly.TryParseExact(raw, "HH:mm", out var time)
            ? time
            : throw new RowingClub.BuildingBlocks.Domain.DomainException(
                "invalid_time", "Saatler SS:dd biçiminde olmalıdır.");
}

internal static class SettingsMapper
{
    public static CompanySettingsDto ToDto(CompanySettings settings) => new(
        settings.OpeningTime.ToString("HH:mm"),
        settings.ClosingTime.ToString("HH:mm"),
        settings.SlotMinutes,
        Enumerable.Range(0, 7).Where(d => settings.IsOpenOn((DayOfWeek)d)).ToList(),
        settings.MinNoticeHours,
        settings.MaxAdvanceDays,
        settings.ReminderOptions().ToList(),
        settings.DefaultReminderMinutes,
        settings.TimeZoneId);
}
