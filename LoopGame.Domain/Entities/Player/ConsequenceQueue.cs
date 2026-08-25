namespace LoopGame.Domain.Entities.Player;

/// <summary>
/// Per-player runtime queue of pending consequences.
/// Inserted when a player selects a Choice with a linked Consequence.
/// Consumed (status → 'fired') at shift start.
/// </summary>
public class ConsequenceQueue
{
    public int       QueueId        { get; set; }
    public int       PlayerId       { get; set; }
    public int       ConsequenceId  { get; set; }
    public ConsequenceStatus Status { get; set; } = ConsequenceStatus.pending;

    public DateTime  QueuedAt       { get; set; } = DateTime.UtcNow;
    public DateTime? FiredAt        { get; set; }

    // Navigation
    public Player      Player      { get; set; } = null!;
    public Consequence Consequence { get; set; } = null!;
}
