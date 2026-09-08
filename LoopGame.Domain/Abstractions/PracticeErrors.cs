namespace LoopGame.Domain.Abstractions;

/// <summary>
/// Domain-level error constants for the Practice/Gate submission flow.
/// </summary>
public static class PracticeErrors
{
    public static readonly Error PlayerNotFound =
        new("Forbidden.AccessGame", "You are not allowed to access this game.");

    public static readonly Error ShiftNotFound =
        new("NotFound.Shift", "Shift Not Found.");

    public static readonly Error NoActiveShift =
        new("Forbidden.Access", "Player has no active shift.");

    public static readonly Error TaskNotInShift =
        new("Forbidden.Access", "You are not allowed to access this task.");

    public static readonly Error TaskNotFound =
        new("NotFound.Task", "The requested practice task was not found.");

    public static readonly Error MaxAttemptsReached =
        new("Practice.MaxAttemptsReached", "Maximum attempts reached for this task.");

    public static readonly Error ProgressNotFound =
        new("NotFound.Progress", "Player shift progress record was not found.");

    public static readonly Error InvalidTaskOrder =
        new("Task.InvalidTaskOrder", "TaskOrder must be between 1 and 255.");

    public static readonly Error InvalidTitle =
        new("Task.InvalidTitle", "Title is required and cannot be empty.");

    public static readonly Error InvalidDescription =
        new("Task.InvalidDescription", "Description is required and cannot be empty.");

    public static readonly Error InvalidConceptTag =
        new("Task.InvalidConceptTag", "ConceptTag is required and cannot be empty.");

    public static readonly Error NegativeEgpReward =
        new("Task.NegativeEgpReward", "EgpReward Must be Positive number.");

    public static readonly Error MaxAttemptsInvalid =
        new("Task.MaxAttemptsInvalid", "MaxAttempts must be between 0 and 32767.");


}
