using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;
using RowingClub.Scheduling.Domain.Settings;

namespace RowingClub.Scheduling.Application.Members;

public sealed record CancelAppointmentCommand(Guid AppointmentId, Guid? CustomerId) : ICommand<Unit>;

public sealed class CancelAppointmentCommandHandler(
    IAppointmentRepository appointmentRepository,
    ICustomerPackageRepository customerPackageRepository,
    IMemberLogRepository memberLogRepository,
    ISettingsRepository settingsRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<CancelAppointmentCommand, Unit>
{
    public async Task<Unit> Handle(CancelAppointmentCommand request, CancellationToken cancellationToken)
    {
        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment", request.AppointmentId.ToString());

        if (request.CustomerId is { } customerId && appointment.CustomerId != customerId)
            throw new DomainException("forbidden", "Bu randevu size ait değil.");

        if (appointment.Status == AppointmentStatus.Cancelled)
            return Unit.Value;

        if (request.CustomerId is not null)
        {
            var settings = await settingsRepository.GetAsync(cancellationToken) ?? CompanySettings.Default();
            settings.EnsureCancellable(appointment.Date, appointment.StartTime);
        }

        if (!appointment.SetStatus(AppointmentStatus.Cancelled))
            return Unit.Value;

        if (appointment.CustomerPackageId is { } packageId)
        {
            var package = await customerPackageRepository.GetByIdAsync(packageId, cancellationToken);
            if (package is not null)
            {
                package.Refund();
                memberLogRepository.Add(MemberLog.Record(
                    appointment.CustomerId, MemberEvents.PackageRefunded,
                    $"{package.PackageName}: {appointment.Date:yyyy-MM-dd} {appointment.StartTime:HH\\:mm} iadesi"));
            }
        }

        memberLogRepository.Add(MemberLog.Record(
            appointment.CustomerId, MemberEvents.AppointmentCancelled,
            $"{appointment.Date:yyyy-MM-dd} {appointment.StartTime:HH\\:mm}"));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
