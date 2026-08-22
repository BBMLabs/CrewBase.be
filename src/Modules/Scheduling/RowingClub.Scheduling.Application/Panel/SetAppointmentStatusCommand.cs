using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Members;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record SetAppointmentStatusCommand(Guid AppointmentId, string Status) : ICommand<Unit>;

public sealed class SetAppointmentStatusCommandHandler(
    IAppointmentRepository appointmentRepository,
    ICustomerPackageRepository customerPackageRepository,
    IMemberLogRepository memberLogRepository,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SetAppointmentStatusCommand, Unit>
{
    public async Task<Unit> Handle(SetAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AppointmentStatus>(request.Status, ignoreCase: true, out var status))
            throw new DomainException(
                "invalid_status", "Geçersiz durum. Pending, Confirmed, Cancelled veya Completed olmalıdır.");

        var appointment = await appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment", request.AppointmentId.ToString());

        var wasCancelled = appointment.Status == AppointmentStatus.Cancelled;
        if (!appointment.SetStatus(status))
            return Unit.Value;

        // Paket bakiyesi durum geçişini izler: iptalde iade, iptalden geri açmada tekrar düşüm.
        if (appointment.CustomerPackageId is { } packageId)
        {
            var package = await customerPackageRepository.GetByIdAsync(packageId, cancellationToken);
            if (package is not null)
            {
                if (status == AppointmentStatus.Cancelled)
                {
                    package.Refund();
                    memberLogRepository.Add(MemberLog.Record(
                        appointment.CustomerId, MemberEvents.PackageRefunded, package.PackageName));
                }
                else if (wasCancelled)
                {
                    package.Deduct();
                    memberLogRepository.Add(MemberLog.Record(
                        appointment.CustomerId, MemberEvents.PackageDeducted,
                        $"{package.PackageName}: kalan {package.RemainingSessions} ders"));
                }
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
