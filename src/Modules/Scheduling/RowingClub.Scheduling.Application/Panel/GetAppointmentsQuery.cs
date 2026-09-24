using MediatR;
using RowingClub.Scheduling.Application.Rsvp;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;

namespace RowingClub.Scheduling.Application.Panel;

/// <summary>Firma panelindeki randevu listesi; tarih verilmezse tümü döner.</summary>
public sealed record GetAppointmentsQuery(DateOnly? Date) : IRequest<List<AppointmentDto>>;

public sealed record AppointmentDto(
    Guid Id,
    Guid SessionId,
    string CustomerName,
    string CustomerPhone,
    string? CustomerEmail,
    int CustomerLevel,
    DateOnly Date,
    string StartTime,
    string BoatClass,
    string? TeammateName,
    string? Note,
    string Status,
    int? ReminderMinutes,
    DateTimeOffset CreatedAtUtc,
    bool UsedPackage,
    string? RsvpChoice,
    DateTimeOffset? RsvpDeadlineUtc);

public sealed class GetAppointmentsQueryHandler(IAppointmentRepository appointmentRepository)
    : IRequestHandler<GetAppointmentsQuery, List<AppointmentDto>>
{
    public async Task<List<AppointmentDto>> Handle(
        GetAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var appointments = request.Date is { } date
            ? await appointmentRepository.GetByDateAsync(date, cancellationToken)
            : await appointmentRepository.GetAllAsync(cancellationToken);

        return appointments
            .OrderBy(a => a.Date).ThenBy(a => a.StartTime)
            .Select(a => new AppointmentDto(
                a.Id, a.SessionId, a.Customer.FullName, a.Customer.Phone, a.Customer.Email,
                a.Customer.Level, a.Date, a.StartTime.ToString("HH:mm"),
                a.Session.BoatClass.Label(), a.TeammateName, a.Note, a.Status.ToString(),
                a.ReminderMinutes, a.CreatedAtUtc, a.UsedPackage,
                RsvpMapper.ToApiChoice(a), a.RsvpDeadlineUtc))
            .ToList();
    }
}
