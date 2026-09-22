using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Sessions;

namespace RowingClub.Scheduling.Domain.Appointments;

public enum AppointmentStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
    Completed = 3,
}

/// <summary>
/// Bir üyenin bir antrenman seansındaki koltuğu. Tarih/saat, hatırlatma sorguları için seanstan
/// denormalize tutulur.
/// </summary>
public sealed class Appointment
{
    public Guid Id { get; private set; }

    public Guid SessionId { get; private set; }

    public TrainingSession Session { get; private set; } = null!;

    public Guid CustomerId { get; private set; }

    public Customer Customer { get; private set; } = null!;

    public DateOnly Date { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public string? Note { get; private set; }

    public string? TeammateName { get; private set; }

    public AppointmentStatus Status { get; private set; }

    /// <summary>Ders başlangıcından kaç dakika önce hatırlatma gönderileceği (null = hatırlatma yok).</summary>
    public int? ReminderMinutes { get; private set; }

    public DateTimeOffset? ReminderSentAtUtc { get; private set; }

    /// <summary>Randevu bir üye paketinden düşüldüyse bakiyenin izlendiği kayıt (iptalde iade edilir).</summary>
    public Guid? CustomerPackageId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private Appointment()
    {
    }

    public static Appointment Book(
        Customer customer, TrainingSession session, string? note,
        int? reminderMinutes, Guid? customerPackageId, string? teammateName = null)
    {
        if (!session.HasFreeSeat)
            throw new DomainException("session_full", "Bu seansta boş koltuk kalmadı.");

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Session = session,
            CustomerId = customer.Id,
            Customer = customer,
            Date = session.Date,
            StartTime = session.StartTime,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            TeammateName = string.IsNullOrWhiteSpace(teammateName) ? null : teammateName.Trim(),
            Status = AppointmentStatus.Pending,
            ReminderMinutes = reminderMinutes,
            CustomerPackageId = customerPackageId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        session.Appointments.Add(appointment);
        return appointment;
    }

    /// <summary>Durumu değiştirir; gerçekten değiştiyse true (paket iade/tekrar düşüm kararı için).</summary>
    public bool SetStatus(AppointmentStatus status)
    {
        if (Status == status)
            return false;

        Status = status;
        return true;
    }

    public void MarkReminderSent() => ReminderSentAtUtc = DateTimeOffset.UtcNow;

    public void MoveToSession(TrainingSession targetSession)
    {
        if (targetSession.Id == SessionId)
            return;

        if (Status == AppointmentStatus.Cancelled)
            throw new DomainException("appointment_cancelled", "İptal edilmiş randevu taşınamaz.");

        if (targetSession.BoatClass != Session.BoatClass)
            throw new DomainException("boat_class_mismatch", "Randevu farklı tekne sınıfındaki bir seansa taşınamaz.");

        if (!targetSession.HasFreeSeat)
            throw new DomainException("session_full", "Hedef seansta boş koltuk kalmadı.");

        Session.Appointments.Remove(this);
        SessionId = targetSession.Id;
        Session = targetSession;
        Date = targetSession.Date;
        StartTime = targetSession.StartTime;
        targetSession.Appointments.Add(this);
    }
}
