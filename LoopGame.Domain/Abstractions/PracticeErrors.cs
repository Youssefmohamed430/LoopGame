namespace LoopGame.Domain.Abstractions;

/// <summary>
/// Domain-level error constants for the Practice/Gate submission flow.
/// </summary>
public static class PracticeErrors
{
    public static readonly Error PlayerNotFound =
        new("Forbidden.AccessGame", "You are not allowed to access this game.");

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
}
