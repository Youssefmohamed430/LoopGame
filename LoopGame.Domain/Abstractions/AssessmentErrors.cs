namespace LoopGame.Domain.Abstractions;

public static class AssessmentErrors
{
    public static readonly Error PlayerNotFound =
        new("Assessment.PlayerNotFound", "Player was not found.");

    public static readonly Error ShiftNotFound =
        new("Assessment.ShiftNotFound", "Shift was not found.");

    public static readonly Error NoEventsFound =
        new("Assessment.NoEventsFound", "No assessment events found for the specified criteria.");

    public static readonly Error DuplicateEvent =
        new("Assessment.DuplicateEvent", "An assessment event with this identifier has already been recorded.");

    public static readonly Error InvalidEventType =
        new("Assessment.InvalidEventType", "The provided event type is not supported.");
}
