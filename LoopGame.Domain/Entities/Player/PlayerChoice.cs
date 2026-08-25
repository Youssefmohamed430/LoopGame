namespace LoopGame.Domain.Entities.Player;

/// <summary>
/// Immutable record of every choice made by a player during gameplay.
/// Once inserted, this record is never updated.
/// </summary>
public class PlayerChoice
{
    public int        PlayerChoiceId     { get; set; }
    public required int       PlayerId  { get; set; }
    public required int        BeatId   { get; set; }
    public required int        ChoiceId { get; set; }
    public ChoiceTier          Tier     { get; set; }
    public DateTime            ChosenAt { get; set; } = DateTime.UtcNow;

    /// <summary>Optional session context stored as JSON string.</summary>
    public string?    SessionContext  { get; set; }

    // Navigation
    public Player     Player { get; set; } = null!;
    public StoryBeat  Beat   { get; set; } = null!;
    public Choice     Choice { get; set; } = null!;
}
