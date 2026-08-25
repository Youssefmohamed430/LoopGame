namespace LoopGame.Domain.Entities.Narrative;

/// <summary>
/// Lightweight pointer linking a Choice to a consequence StoryBeat.
/// All consequence content lives in the StoryBeat row itself.
/// The target shift is derived from StoryBeat.ShiftId.
/// </summary>
public class Consequence
{
    public int    ConsequenceId  { get; set; }
    public int    BeatId         { get; set; }

    /// <summary>'start' = prepend to narrative; 'end' = append.</summary>
    public string InjectPosition { get; set; } = "start";

    // Navigation
    public StoryBeat                              Beat              { get; set; } = null!;
    public ICollection<Player.ConsequenceQueue>   ConsequenceQueues { get; set; } = [];
}
