namespace LoopGame.Domain.Entities.Player;

/// <summary>
/// Tracks a player's progression state through a single shift.
/// Composite unique key: (PlayerId, ShiftId).
/// </summary>
public class PlayerShiftProgress
{
    public int                ProgressId    { get; set; }
    public int                PlayerId      { get; set; }
    public int                ShiftId       { get; set; }
    public ShiftProgressStatus Status       { get; set; } = ShiftProgressStatus.InProgress;
    public DateTime?          StartedAt     { get; set; }
    public DateTime?          CompletedAt   { get; set; }
    public short              GateAttempts  { get; set; } = 0;

    // Navigation
    public Player Player { get; set; } = null!;
    public Shift  Shift  { get; set; } = null!;
}
