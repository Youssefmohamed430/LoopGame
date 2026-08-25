namespace LoopGame.Domain.Entities.SideTask;

/// <summary>
/// Stores progressive AI-generated hints (levels 1–3) for an active PlayerSideTask.
/// One side task can have up to 3 hints (1:N). Cascade deleted with the parent task.
/// </summary>
public class SideTaskHint
{
    public int       HintId      { get; set; }
    public int       SideTaskId  { get; set; }

    /// <summary>1 = Conceptual Nudge, 2 = Structural Guidance, 3 = Code Snippet.</summary>
    public HintLevel HintLevel   { get; set; }

    public string    HintText    { get; set; } = string.Empty;
    public decimal   EgpCost     { get; set; } = 0m;
    public bool      IsUnlocked  { get; set; } = false;
    public DateTime? UnlockedAt  { get; set; }
    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;

    // Navigation
    public PlayerSideTask SideTask { get; set; } = null!;
}
