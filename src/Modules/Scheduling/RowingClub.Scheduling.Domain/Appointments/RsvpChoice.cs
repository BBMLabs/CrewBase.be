namespace RowingClub.Scheduling.Domain.Appointments;

public enum RsvpChoice
{
    None = 0,
    Attending = 1,
    NotAttending = 2,
}

public static class RsvpChoiceExtensions
{
    public static string ToApiValue(this RsvpChoice choice) => choice switch
    {
        RsvpChoice.Attending => "attending",
        RsvpChoice.NotAttending => "notAttending",
        _ => "none",
    };

    public static bool TryParseApiValue(string? value, out RsvpChoice choice)
    {
        switch (value)
        {
            case "attending":
                choice = RsvpChoice.Attending;
                return true;
            case "notAttending":
                choice = RsvpChoice.NotAttending;
                return true;
            default:
                choice = RsvpChoice.None;
                return false;
        }
    }
}
