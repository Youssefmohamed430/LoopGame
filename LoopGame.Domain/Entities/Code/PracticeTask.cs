namespace LoopGame.Domain.Entities.Code;

/// <summary>
/// Mandatory coding exercise within a shift gate.
/// Difficulty tiers: SpacedRetrieval, Standard, Challenge.
/// </summary>
public class PracticeTask
{
    public int      TaskId      { get; set; }
    public int      ShiftId     { get; set; }
    public byte     TaskOrder   { get; set; }
    public string   Title       { get; set; } = string.Empty;
    public string   Description { get; set; } = string.Empty;
    public string?  StarterCode { get; set; }
    public string   ConceptTag  { get; set; } = string.Empty;

    /// <summary>SpacedRetrieval | Standard | Challenge</summary>
    public string   Difficulty  { get; set; } = "Standard";

    public short    MaxAttempts { get; set; } = 0;
    public decimal  EgpReward   { get; set; } = 0m;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    // Navigation
    public Shift                        Shift    { get; set; } = null!;
    public ICollection<TestCase>         TestCases { get; set; } = [];
    public ICollection<PracticeAttempt>  Attempts  { get; set; } = [];
}
