namespace LoopGame.Domain.Entities.Player;

/// <summary>
/// Stores a full serialized LoopOS desktop state for a specific player save slot.
/// Composite unique key: (PlayerId, SlotNumber). Slot numbers are 1, 2, or 3.
/// </summary>
public class PlayerSave
{
    public int           SaveId       { get; set; }
    public int           PlayerId     { get; set; }
    public byte SlotNumber { get; set; } // 1, 2, or 3
    public string?       SaveLabel    { get; set; }

    /// <summary>Stored as JSON via EF Core OwnsOne().ToJson().</summary>
    public DesktopState  DesktopState { get; set; } = null!;

    public DateTime      SavedAt      { get; set; } = DateTime.UtcNow;

    // Navigation
    public Player Player { get; set; } = null!;
}
