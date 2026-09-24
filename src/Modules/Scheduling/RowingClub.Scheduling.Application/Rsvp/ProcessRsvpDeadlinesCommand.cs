using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.Scheduling.Application.Rsvp;

public sealed record ProcessRsvpDeadlinesCommand : ICommand<RsvpResolutionResult>;

public sealed record RsvpResolutionResult(int Confirmed, int Cancelled, int Skipped);

public sealed class ProcessRsvpDeadlinesCommandHandler(
    IAppointmentRepository appointmentRepository,
    ICustomerPackageRepository customerPackageRepository,
    IMemberLogRepository memberLogRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<ProcessRsvpDeadlinesCommand, RsvpResolutionResult>
{
    public const int BatchSize = 200;

    public async Task<RsvpResolutionResult> Handle(ProcessRsvpDeadlinesCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var due = await appointmentRepository.GetDueRsvpsAsync(nowUtc, BatchSize, cancellationToken);
        if (due.Count == 0)
            return new RsvpResolutionResult(0, 0, 0);

        int confirmed = 0, cancelled = 0, skipped = 0;
        foreach (var appointment in due)
        {
            switch (appointment.ResolveRsvp(nowUtc))
            {
                case AppointmentStatus.Confirmed:
                    confirmed++;
                    break;
                case AppointmentStatus.Cancelled:
                    await RecordCancellationAsync(appointment, cancellationToken);
                    cancelled++;
                    break;
                default:
                    skipped++;
                    break;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RsvpResolutionResult(confirmed, cancelled, skipped);
    }

    private async Task RecordCancellationAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        var slot = $"{appointment.Date:yyyy-MM-dd} {appointment.StartTime:HH\\:mm}";

        if (appointment.CustomerPackageId is { } packageId)
        {
            var package = await customerPackageRepository.GetByIdAsync(packageId, cancellationToken);
            if (package is not null)
            {
                package.Refund();
                memberLogRepository.Add(MemberLog.Record(
                    appointment.CustomerId, MemberEvents.PackageRefunded,
                    $"{package.PackageName}: {slot} iadesi"));
            }
        }

        memberLogRepository.Add(MemberLog.Record(
            appointment.CustomerId, MemberEvents.AppointmentCancelled, slot));

        memberLogRepository.Add(MemberLog.Record(
            appointment.CustomerId, MemberEvents.AppointmentCancelledByRsvp,
            $"{slot}: katılım onayında \"Katılamıyorum\" yanıtı verildiği için iptal edildi"));
    }
}
