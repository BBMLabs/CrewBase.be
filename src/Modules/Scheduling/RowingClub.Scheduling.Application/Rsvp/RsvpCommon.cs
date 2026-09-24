using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;

namespace RowingClub.Scheduling.Application.Rsvp;

public sealed record RsvpDto(
    string Date,
    string StartTime,
    string BoatClass,
    string ClubName,
    string FirstName,
    string Choice,
    DateTimeOffset? DeadlineUtc,
    bool Open,
    string Status);

public interface IAppointmentRsvpEmailSender
{
    Task SendAsync(
        string email, string firstName, string companyName, DateOnly date, TimeOnly startTime,
        string boatClass, string rsvpToken, string subdomain, CancellationToken cancellationToken);
}

public static class RsvpMapper
{
    public static RsvpDto ToDto(Appointment appointment, string clubName, DateTimeOffset nowUtc) => new(
        appointment.Date.ToString("yyyy-MM-dd"),
        appointment.StartTime.ToString("HH:mm"),
        appointment.Session.BoatClass.Label(),
        clubName,
        FirstName(appointment.Customer.FullName),
        (appointment.RsvpChoice ?? RsvpChoice.None).ToApiValue(),
        appointment.RsvpDeadlineUtc,
        appointment.IsRsvpOpen(nowUtc),
        appointment.Status.ToString());

    public static bool IsValidChoice(string? value) => RsvpChoiceExtensions.TryParseApiValue(value, out _);

    public static string? ToApiChoice(Appointment appointment) =>
        appointment.HasRsvp ? (appointment.RsvpChoice ?? RsvpChoice.None).ToApiValue() : null;

    public static string FirstName(string fullName)
    {
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "",
            1 => parts[0],
            _ => string.Join(' ', parts[..^1]),
        };
    }
}
