using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Scheduling.Application.Booking;

/// <summary>
/// Public sitedeki randevu formundan gelir; tenant DB'si API katmanında çözülmüş olmalıdır.
/// Üye, derecesine ve seçtiği tekne sınıfına (1x/2x/4x) göre uygun seansa gruplanır.
/// </summary>
public sealed record BookAppointmentCommand(
    string FullName,
    string Phone,
    string? Email,
    DateOnly Date,
    TimeOnly StartTime,
    string BoatClass,
    string? Note,
    int? ReminderMinutes,
    bool UsePackage,
    List<string> AcceptedConsents,
    string? IpAddress) : ICommand<BookAppointmentResponse>;

public sealed record BookAppointmentResponse(
    Guid AppointmentId,
    Guid SessionId,
    DateOnly Date,
    TimeOnly StartTime,
    string BoatClass,
    int Level,
    string? BoatName,
    string? InstructorName,
    int? ReminderMinutes,
    string Status);
