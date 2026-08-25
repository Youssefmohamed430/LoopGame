namespace LoopGame.Domain.Entities.Assessment;

/// <summary>
/// Computed mastery score for a specific CS111 concept per player per shift.
/// mastery_score is DECIMAL(5,4) in range [0, 1].
/// </summary>
public class ConceptMasterySnapshot
{
    public int      SnapshotId     { get; set; }
    public int      PlayerId       { get; set; }
    public int      ShiftId        { get; set; }
    public string   ConceptTag     { get; set; } = string.Empty;
    public decimal  MasteryScore   { get; set; } // [0, 1]
    public int      EvidenceCount  { get; set; } = 0;
    public DateTime SnapshottedAt  { get; set; } = DateTime.UtcNow;

    // Navigation
    public Player.Player Player { get; set; } = null!;
    public Shift          Shift  { get; set; } = null!;
}
