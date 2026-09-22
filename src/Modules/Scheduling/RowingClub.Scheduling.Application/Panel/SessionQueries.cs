using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Instructors;
using RowingClub.Scheduling.Domain.Sessions;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record SessionMemberDto(Guid AppointmentId, string FullName, string Phone, int Level, string Status);

public sealed record SessionDto(
    Guid Id,
    DateOnly Date,
    string StartTime,
    string BoatClass,
    int Level,
    int Capacity,
    int MemberCount,
    string? BoatName,
    string? InstructorName,
    List<SessionMemberDto> Members);

/// <summary>Panel: günün seansları - kim hangi teknede, hangi hocayla, hangi derece grubunda.</summary>
public sealed record GetSessionsQuery(DateOnly Date) : IRequest<List<SessionDto>>;

/// <summary>Panel: seansın teknesini/eğitmenini elle değiştirme (otomatik atamayı ezer).</summary>
public sealed record AssignSessionResourcesCommand(Guid SessionId, Guid? BoatId, Guid? InstructorId)
    : ICommand<SessionDto>;

public sealed record MoveAppointmentToSessionCommand(Guid AppointmentId, Guid TargetSessionId) : ICommand<Unit>;

public sealed class GetSessionsQueryHandler(ITrainingSessionRepository sessionRepository)
    : IRequestHandler<GetSessionsQuery, List<SessionDto>>
{
    public async Task<List<SessionDto>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await sessionRepository.GetByDateAsync(request.Date, cancellationToken);
        return sessions
            .OrderBy(s => s.StartTime).ThenBy(s => s.Level)
            .Select(SessionMapper.ToDto)
            .ToList();
    }
}

public sealed class AssignSessionResourcesCommandHandler(
    ITrainingSessionRepository sessionRepository,
    IBoatRepository boatRepository,
    IInstructorRepository instructorRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<AssignSessionResourcesCommand, SessionDto>
{
    public async Task<SessionDto> Handle(AssignSessionResourcesCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken)
            ?? throw new NotFoundException("TrainingSession", request.SessionId.ToString());

        var slotSessions = await sessionRepository.GetBySlotAsync(session.Date, session.StartTime, cancellationToken);

        if (request.BoatId is { } boatId)
        {
            var boat = await boatRepository.GetByIdAsync(boatId, cancellationToken)
                ?? throw new NotFoundException("Boat", boatId.ToString());

            var takenByOther = slotSessions.Any(s => s.Id != session.Id && s.BoatId == boat.Id);
            if (takenByOther)
                throw new DomainException("boat_taken", "Bu tekne aynı saatte başka bir seansa atanmış.");

            session.AssignBoat(boat);
        }

        if (request.InstructorId is { } instructorId)
        {
            var instructor = await instructorRepository.GetByIdAsync(instructorId, cancellationToken)
                ?? throw new NotFoundException("Instructor", instructorId.ToString());

            var busyElsewhere = slotSessions.Any(s => s.Id != session.Id && s.InstructorId == instructor.Id);
            if (busyElsewhere)
                throw new DomainException("instructor_busy", "Bu eğitmen aynı saatte başka bir seansta.");

            session.AssignInstructor(instructor);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return SessionMapper.ToDto(session);
    }
}

public sealed class MoveAppointmentToSessionCommandHandler(
    IAppointmentRepository appointmentRepository,
    ITrainingSessionRepository sessionRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<MoveAppointmentToSessionCommand, Unit>
{
    public async Task<Unit> Handle(MoveAppointmentToSessionCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment", request.AppointmentId.ToString());

        var targetSession = await sessionRepository.GetByIdAsync(request.TargetSessionId, cancellationToken)
            ?? throw new NotFoundException("TrainingSession", request.TargetSessionId.ToString());

        appointment.MoveToSession(targetSession);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

internal static class SessionMapper
{
    public static SessionDto ToDto(TrainingSession session) => new(
        session.Id,
        session.Date,
        session.StartTime.ToString("HH:mm"),
        session.BoatClass.Label(),
        session.Level,
        session.Capacity,
        session.ActiveMemberCount,
        session.Boat?.Name,
        session.Instructor?.FullName,
        session.Appointments
            .Where(a => a.Status != AppointmentStatus.Cancelled)
            .Select(a => new SessionMemberDto(
                a.Id, a.Customer.FullName, a.Customer.Phone, a.Customer.Level, a.Status.ToString()))
            .ToList());
}
