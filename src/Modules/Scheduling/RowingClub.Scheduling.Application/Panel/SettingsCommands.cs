using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record DayScheduleDto(int Day, bool IsOpen, string OpeningTime, string ClosingTime);

public sealed record CompanySettingsDto(
    List<DayScheduleDto> WorkingHours,
    int SlotMinutes,
    int MinNoticeHours,
    int MaxAdvanceDays,
    List<int> ReminderOptions,
    int DefaultReminderMinutes,
    string TimeZoneId,
    bool NotifyOnNewAppointment,
    bool NotifyOnCancellation,
    bool SendCustomerReminders,
    List<int> PackageExpiryReminderDays);

public sealed record GetSettingsQuery : IRequest<CompanySettingsDto>;

/// <summary>Firma çalışma düzenini ve randevu/hatırlatma/bildirim kurallarını günceller (tamamı dinamik).</summary>
public sealed record UpdateSettingsCommand(
    List<DayScheduleDto> WorkingHours,
    int SlotMinutes,
    int MinNoticeHours,
    int MaxAdvanceDays,
    List<int> ReminderOptions,
    int DefaultReminderMinutes,
    string TimeZoneId,
    bool NotifyOnNewAppointment,
    bool NotifyOnCancellation,
    bool SendCustomerReminders,
    List<int> PackageExpiryReminderDays) : ICommand<CompanySettingsDto>;

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

        var days = request.WorkingHours
            .Select(d => ((DayOfWeek)d.Day, d.IsOpen, ParseTime(d.OpeningTime), ParseTime(d.ClosingTime)))
            .ToList();

        settings.UpdateWorkingHours(days, request.SlotMinutes);
        settings.UpdateBookingRules(
            request.MinNoticeHours, request.MaxAdvanceDays, request.ReminderOptions, request.DefaultReminderMinutes,
            request.TimeZoneId);
        settings.UpdateNotifications(
            request.NotifyOnNewAppointment, request.NotifyOnCancellation, request.SendCustomerReminders);
        settings.UpdatePackageExpirySettings(request.PackageExpiryReminderDays);

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
        settings.DaySchedules
            .OrderBy(d => (int)d.Day)
            .Select(d => new DayScheduleDto(
                (int)d.Day, d.IsOpen, d.OpeningTime.ToString("HH:mm"), d.ClosingTime.ToString("HH:mm")))
            .ToList(),
        settings.SlotMinutes,
        settings.MinNoticeHours,
        settings.MaxAdvanceDays,
        settings.ReminderOptions().ToList(),
        settings.DefaultReminderMinutes,
        settings.TimeZoneId,
        settings.NotifyOnNewAppointment,
        settings.NotifyOnCancellation,
        settings.SendCustomerReminders,
        settings.PackageExpiryReminderDays().ToList());
}
