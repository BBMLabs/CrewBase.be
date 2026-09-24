using Microsoft.EntityFrameworkCore;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Infrastructure.Persistence;

namespace RowingClub.Scheduling.Infrastructure.Repositories;

public sealed class AppointmentRepository(TenantDbContext context) : IAppointmentRepository
{
    public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Session)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<List<Appointment>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken) =>
        context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Session)
            .Where(a => a.Date == date)
            .ToListAsync(cancellationToken);

    public Task<List<Appointment>> GetAllAsync(CancellationToken cancellationToken) =>
        context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Session)
            .ToListAsync(cancellationToken);

    public Task<bool> HasActiveForCustomerAtAsync(
        Guid customerId, DateOnly date, TimeOnly startTime, CancellationToken cancellationToken) =>
        context.Appointments.AnyAsync(
            a => a.CustomerId == customerId && a.Date == date && a.StartTime == startTime &&
                 a.Status != AppointmentStatus.Cancelled,
            cancellationToken);

    public Task<List<Appointment>> GetPendingRemindersAsync(
        DateOnly fromDate, CancellationToken cancellationToken) =>
        context.Appointments
            .Include(a => a.Customer)
            .Where(a => a.ReminderSentAtUtc == null &&
                        a.ReminderMinutes != null &&
                        a.Status != AppointmentStatus.Cancelled &&
                        a.Date >= fromDate.AddDays(-1) && a.Date <= fromDate.AddDays(8))
            .ToListAsync(cancellationToken);

    public Task<bool> HasActiveFutureAppointmentsUsingPackageAsync(
        Guid customerPackageId, DateOnly today, TimeOnly nowTime, CancellationToken cancellationToken) =>
        context.Appointments.AnyAsync(
            a => a.CustomerPackageId == customerPackageId &&
                 a.Status != AppointmentStatus.Cancelled &&
                 (a.Date > today || (a.Date == today && a.StartTime > nowTime)),
            cancellationToken);

    public async Task<HashSet<Guid>> GetPackageIdsWithActiveFutureAppointmentsAsync(
        IReadOnlyCollection<Guid> customerPackageIds, DateOnly today, TimeOnly nowTime, CancellationToken cancellationToken)
    {
        var ids = await context.Appointments
            .Where(a => a.CustomerPackageId != null && customerPackageIds.Contains(a.CustomerPackageId.Value) &&
                        a.Status != AppointmentStatus.Cancelled &&
                        (a.Date > today || (a.Date == today && a.StartTime > nowTime)))
            .Select(a => a.CustomerPackageId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public Task<Appointment?> GetByRsvpTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Session)
            .FirstOrDefaultAsync(a => a.RsvpTokenHash == tokenHash, cancellationToken);

    public Task<List<Appointment>> GetDueRsvpsAsync(DateTimeOffset nowUtc, int limit, CancellationToken cancellationToken) =>
        context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Session)
            .Where(a => a.RsvpDeadlineUtc != null && a.RsvpResolvedAtUtc == null && a.RsvpDeadlineUtc <= nowUtc)
            .OrderBy(a => a.RsvpDeadlineUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public void Add(Appointment appointment) => context.Appointments.Add(appointment);
}
