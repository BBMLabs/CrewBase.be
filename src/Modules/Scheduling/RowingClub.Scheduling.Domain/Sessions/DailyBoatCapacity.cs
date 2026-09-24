using RowingClub.Scheduling.Domain.Boats;

namespace RowingClub.Scheduling.Domain.Sessions;

public static class DailyBoatCapacity
{
    public static int FreeBoats(IEnumerable<TrainingSession> daySessions, BoatClass boatClass, int activeBoatCount)
    {
        var sessionsOfClass = daySessions.Count(s => s.BoatClass == boatClass);
        return Math.Max(0, activeBoatCount - sessionsOfClass);
    }

    public static int SeatsLeftAt(
        IReadOnlyCollection<TrainingSession> daySessions, BoatClass boatClass, int activeBoatCount, TimeOnly slot)
    {
        var joinableSeats = daySessions
            .Where(s => s.BoatClass == boatClass && s.StartTime == slot)
            .Sum(s => Math.Max(0, s.Capacity - s.ActiveMemberCount));

        return joinableSeats + FreeBoats(daySessions, boatClass, activeBoatCount) * boatClass.Capacity();
    }

    public static Boat? PickFreeBoat(
        IReadOnlyCollection<TrainingSession> daySessions, IReadOnlyCollection<Boat> activeBoatsOfClass, BoatClass boatClass)
    {
        if (FreeBoats(daySessions, boatClass, activeBoatsOfClass.Count) == 0)
            return null;

        var usedBoatIds = daySessions
            .Where(s => s.BoatClass == boatClass && s.BoatId is not null)
            .Select(s => s.BoatId!.Value)
            .ToHashSet();

        return activeBoatsOfClass.FirstOrDefault(b => !usedBoatIds.Contains(b.Id));
    }
}
