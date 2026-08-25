namespace LoopGame.Domain.Entities.Code;

/// <summary>
/// Logs a code submission against a mandatory practice gate task.
/// test_results stored as a JSON array of TestCaseResult.
/// </summary>
public class PracticeAttempt
{
    public int        AttemptId      { get; set; }
    public int        PlayerId       { get; set; }
    public int        TaskId         { get; set; }
    public string     SubmittedCode  { get; set; } = string.Empty;
    public ChoiceTier Tier           { get; set; }

    /// <summary>JSON array of TestCaseResult records.</summary>
    public string     TestResults    { get; set; } = "[]";

    public int        TimeSpentSec   { get; set; } = 0;
    public bool       HintUsed       { get; set; } = false;
    public DateTime   SubmittedAt    { get; set; } = DateTime.UtcNow;

    // Navigation
    public Player.Player  Player { get; set; } = null!;
    public PracticeTask   Task   { get; set; } = null!;
}
