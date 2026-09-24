using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;

namespace RowingClub.Scheduling.Application.Rsvp;

public sealed record GetRsvpQuery(string Token, string ClubName) : IQuery<RsvpDto?>;

public sealed record SubmitRsvpCommand(string Token, string Choice, string ClubName) : ICommand<RsvpSubmitResult>;

public enum RsvpSubmitOutcome
{
    Saved,
    NotFound,
    Closed,
    InvalidChoice,
}

public sealed record RsvpSubmitResult(RsvpSubmitOutcome Outcome, RsvpDto? Rsvp);

public sealed class GetRsvpQueryHandler(
    IAppointmentRepository appointmentRepository,
    IRefreshTokenHasher tokenHasher)
    : IRequestHandler<GetRsvpQuery, RsvpDto?>
{
    public async Task<RsvpDto?> Handle(GetRsvpQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return null;

        var appointment = await appointmentRepository.GetByRsvpTokenHashAsync(
            tokenHasher.Hash(request.Token), cancellationToken);

        return appointment is null ? null : RsvpMapper.ToDto(appointment, request.ClubName, DateTimeOffset.UtcNow);
    }
}

public sealed class SubmitRsvpCommandHandler(
    IAppointmentRepository appointmentRepository,
    IRefreshTokenHasher tokenHasher,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SubmitRsvpCommand, RsvpSubmitResult>
{
    public async Task<RsvpSubmitResult> Handle(SubmitRsvpCommand request, CancellationToken cancellationToken)
    {
        if (!RsvpChoiceExtensions.TryParseApiValue(request.Choice, out var choice))
            return new RsvpSubmitResult(RsvpSubmitOutcome.InvalidChoice, null);

        if (string.IsNullOrWhiteSpace(request.Token))
            return new RsvpSubmitResult(RsvpSubmitOutcome.NotFound, null);

        var appointment = await appointmentRepository.GetByRsvpTokenHashAsync(
            tokenHasher.Hash(request.Token), cancellationToken);
        if (appointment is null)
            return new RsvpSubmitResult(RsvpSubmitOutcome.NotFound, null);

        var nowUtc = DateTimeOffset.UtcNow;
        if (!appointment.IsRsvpOpen(nowUtc))
            return new RsvpSubmitResult(RsvpSubmitOutcome.Closed, RsvpMapper.ToDto(appointment, request.ClubName, nowUtc));

        appointment.ChooseRsvp(choice, nowUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RsvpSubmitResult(RsvpSubmitOutcome.Saved, RsvpMapper.ToDto(appointment, request.ClubName, nowUtc));
    }
}
