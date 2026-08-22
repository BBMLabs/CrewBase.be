using MediatR;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Sessions;

namespace RowingClub.Scheduling.Application.Members;

/// <summary>
/// Üyenin randevu geçmişi + yaklaşanlar. Aynı teknedeki diğer üyeler yalnızca maskeli adla
/// ("Veli D.") görünür - tam ad, telefon veya başka kişisel veri üyelere sızdırılmaz.
/// </summary>
public sealed record GetMemberAppointmentsQuery(Guid CustomerId) : IRequest<List<MemberAppointmentDto>>;

public sealed record MemberAppointmentDto(
    Guid Id,
    DateOnly Date,
    string StartTime,
    string BoatClass,
    string? BoatName,
    string? InstructorName,
    string Status,
    string? Note,
    int? ReminderMinutes,
    bool UsedPackage,
    List<string> Crewmates);

public sealed class GetMemberAppointmentsQueryHandler(
    IAppointmentRepository appointmentRepository,
    ITrainingSessionRepository sessionRepository)
    : IRequestHandler<GetMemberAppointmentsQuery, List<MemberAppointmentDto>>
{
    public async Task<List<MemberAppointmentDto>> Handle(
        GetMemberAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var appointments = (await appointmentRepository.GetAllAsync(cancellationToken))
            .Where(a => a.CustomerId == request.CustomerId)
            .OrderByDescending(a => a.Date).ThenByDescending(a => a.StartTime)
            .ToList();

        var result = new List<MemberAppointmentDto>(appointments.Count);
        foreach (var appointment in appointments)
        {
            // Tekne arkadaşları için seans üyeleri gerekir; GetAll'daki Session nav'ında
            // Appointments koleksiyonu dolu gelmeyebilir, seansı tam yükle.
            var session = await sessionRepository.GetByIdAsync(appointment.SessionId, cancellationToken);

            var crewmates = session is null
                ? []
                : session.Appointments
                    .Where(a => a.CustomerId != request.CustomerId && a.Status != AppointmentStatus.Cancelled)
                    .Select(a => NameMask.Mask(a.Customer.FullName))
                    .ToList();

            result.Add(new MemberAppointmentDto(
                appointment.Id,
                appointment.Date,
                appointment.StartTime.ToString("HH:mm"),
                session?.BoatClass.Label() ?? "-",
                session?.Boat?.Name,
                session?.Instructor?.FullName,
                appointment.Status.ToString(),
                appointment.Note,
                appointment.ReminderMinutes,
                appointment.CustomerPackageId is not null,
                crewmates));
        }

        return result;
    }
}
