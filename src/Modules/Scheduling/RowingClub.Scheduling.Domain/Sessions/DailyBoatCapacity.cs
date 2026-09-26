using RowingClub.Scheduling.Domain.Boats;

namespace RowingClub.Scheduling.Domain.Sessions;

public static class DailyBoatCapacity
{
    public static bool HoldsBoat(TrainingSession session) => session.ActiveMemberCount > 0;

    public static int FreeBoats(IEnumerable<TrainingSession> daySessions, BoatClass boatClass, int activeBoatCount)
    {
        var sessionsOfClass = daySessions.Count(s => s.BoatClass == boatClass && HoldsBoat(s));
        return Math.Max(0, activeBoatCount - sessionsOfClass);
    }

    public static int SeatsLeftAt(
        IReadOnlyCollection<TrainingSession> daySessions, BoatClass boatClass, int activeBoatCount, TimeOnly slot)
    {
        var joinableSeats = daySessions
            .Where(s => s.BoatClass == boatClass && s.StartTime == slot && HoldsBoat(s))
            .Sum(s => Math.Max(0, s.Capacity - s.ActiveMemberCount));

        return joinableSeats + FreeBoats(daySessions, boatClass, activeBoatCount) * boatClass.Capacity();
    }

    public static bool CanReuseEmptySession(
        IReadOnlyCollection<TrainingSession> daySessions, TrainingSession emptySession, int activeBoatCount)
    {
        if (FreeBoats(daySessions, emptySession.BoatClass, activeBoatCount) == 0)
            return false;

        return emptySession.BoatId is null || !BoatsInUse(daySessions, emptySession.BoatClass).Contains(emptySession.BoatId.Value);
    }

    public static Boat? PickFreeBoat(
        IReadOnlyCollection<TrainingSession> daySessions, IReadOnlyCollection<Boat> activeBoatsOfClass, BoatClass boatClass)
    {
        if (FreeBoats(daySessions, boatClass, activeBoatsOfClass.Count) == 0)
            return null;

        var usedBoatIds = BoatsInUse(daySessions, boatClass);
        return activeBoatsOfClass.FirstOrDefault(b => !usedBoatIds.Contains(b.Id));
    }

    private static HashSet<Guid> BoatsInUse(IEnumerable<TrainingSession> daySessions, BoatClass boatClass) =>
        daySessions
            .Where(s => s.BoatClass == boatClass && HoldsBoat(s) && s.BoatId is not null)
            .Select(s => s.BoatId!.Value)
            .ToHashSet();
}
