using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain.Appointments;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Instructors;

namespace RowingClub.Scheduling.Domain.Sessions;

/// <summary>
/// Aynı gün/saatteki, aynı tekne sınıfı ve aynı derecedeki üyeleri bir araya getiren antrenman
/// seansı. Kapasite tekne sınıfından gelir (1x=1, 2x=2, 4x=4); tekne ve eğitmen rezervasyon
/// sırasında otomatik atanır, panelden değiştirilebilir.
/// </summary>
public sealed class TrainingSession
{
    public Guid Id { get; private set; }

    public DateOnly Date { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public BoatClass BoatClass { get; private set; }

    /// <summary>Üye derecesi (0 = derecesiz). Seansa yalnızca aynı derecedeki üyeler katılır.</summary>
    public int Level { get; private set; }

    public Guid? BoatId { get; private set; }

    public Boat? Boat { get; private set; }

    public Guid? InstructorId { get; private set; }

    public Instructor? Instructor { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public List<Appointment> Appointments { get; private set; } = [];

    public int Capacity => BoatClass.Capacity();

    public int ActiveMemberCount =>
        Appointments.Count(a => a.Status != AppointmentStatus.Cancelled);

    public bool HasFreeSeat => ActiveMemberCount < Capacity;

    private TrainingSession()
    {
    }

    public static TrainingSession Create(
        DateOnly date, TimeOnly startTime, BoatClass boatClass, int level,
        Guid? boatId, Guid? instructorId) => new()
    {
        Id = Guid.NewGuid(),
        Date = date,
        StartTime = startTime,
        BoatClass = boatClass,
        Level = level,
        BoatId = boatId,
        InstructorId = instructorId,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };

    public void AssignBoat(Boat boat)
    {
        if (boat.Class != BoatClass)
            throw new DomainException("boat_class_mismatch", "Tekne sınıfı seansın sınıfıyla eşleşmiyor.");

        BoatId = boat.Id;
        Boat = boat;
    }

    public void AssignInstructor(Instructor instructor)
    {
        InstructorId = instructor.Id;
        Instructor = instructor;
    }
}
