namespace LoopGame.Domain.Entities.Narrative;

/// <summary>
/// One of up to 4 choices presented at a narrative choice beat.
/// Optionally links to a Consequence to enqueue a deferred beat in a future shift.
/// </summary>
public class Choice
{
    public int        ChoiceId          { get; set; }
    public int        BeatId            { get; set; }
    public byte       ChoiceIndex       { get; set; } // 1–4
    public string     ChoiceText        { get; set; } = string.Empty;
    public ChoiceTier Tier              { get; set; }
    public int?       ConsequenceId     { get; set; }
    public string?    ImmediateFeedback { get; set; }

    // Navigation
    public StoryBeat    Beat        { get; set; } = null!;
    public Consequence? Consequence { get; set; }
}
