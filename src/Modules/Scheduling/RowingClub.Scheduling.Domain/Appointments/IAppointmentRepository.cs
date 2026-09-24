namespace RowingClub.Scheduling.Domain.Appointments;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<List<Appointment>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken);

    Task<List<Appointment>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Aynı üyenin aynı slotta ikinci kez yer almasını engellemek için.</summary>
    Task<bool> HasActiveForCustomerAtAsync(
        Guid customerId, DateOnly date, TimeOnly startTime, CancellationToken cancellationToken);

    /// <summary>Hatırlatması henüz gönderilmemiş, iptal edilmemiş, bugünden itibaren yaklaşan randevular.</summary>
    Task<List<Appointment>> GetPendingRemindersAsync(DateOnly fromDate, CancellationToken cancellationToken);

    /// <summary>
    /// Bu paketi kullanan, iptal edilmemiş, henüz gerçekleşmemiş (gelecekteki) bir randevu var mı -
    /// süresi dolan bir CustomerPackage'ı silmeden önce bu kontrol yapılır (bkz. ProcessPackageExpiriesCommand).
    /// </summary>
    Task<bool> HasActiveFutureAppointmentsUsingPackageAsync(
        Guid customerPackageId, DateOnly today, TimeOnly nowTime, CancellationToken cancellationToken);

    /// <summary>Verilenler arasından, iptal edilmemiş gelecekteki bir randevuda kullanılan paket kimlikleri.</summary>
    Task<HashSet<Guid>> GetPackageIdsWithActiveFutureAppointmentsAsync(
        IReadOnlyCollection<Guid> customerPackageIds, DateOnly today, TimeOnly nowTime, CancellationToken cancellationToken);

    Task<Appointment?> GetByRsvpTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<List<Appointment>> GetDueRsvpsAsync(DateTimeOffset nowUtc, int limit, CancellationToken cancellationToken);

    void Add(Appointment appointment);
}
