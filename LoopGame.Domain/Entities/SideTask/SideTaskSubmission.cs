namespace LoopGame.Domain.Entities.SideTask;

/// <summary>
/// Submission log for a player's code attempt against an AI side task.
/// test_results stored as a JSON array of TestCaseResult.
/// </summary>
public class SideTaskSubmission
{
    public int        SubmissionId   { get; set; }
    public int        SideTaskId     { get; set; }
    public int        PlayerId       { get; set; }
    public string     SubmittedCode  { get; set; } = string.Empty;
    public ChoiceTier Tier           { get; set; }

    /// <summary>JSON array of TestCaseResult records.</summary>
    public string     TestResults    { get; set; } = "[]";

    public byte       SahmHintsUsed  { get; set; } = 0;
    public int        TimeSpentSec   { get; set; } = 0;
    public decimal    EgpEarned      { get; set; } = 0m;
    public DateTime   SubmittedAt    { get; set; } = DateTime.UtcNow;

    // Navigation
    public PlayerSideTask   SideTask { get; set; } = null!;
    public Player.Player    Player   { get; set; } = null!;
}
