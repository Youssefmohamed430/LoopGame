namespace LoopGame.Application.Dtos.NarrativeDtos;

/// <summary>
/// Request model for creating a new StoryBeat (admin content management).
/// BeatType drives which fields are required:
///   - Narrative:    SequenceOrder is required; ShiftId is the owning shift.
///   - Consequence:  SequenceOrder must be null; InjectPosition is required.
/// </summary>
public class CreateStoryBeatDto
{
    /// <summary>The shift this beat belongs to.</summary>
    public int     ShiftId       { get; set; }

    /// <summary>Globally unique identifier for Ink bridge lookup.</summary>
    public string  BeatKey       { get; set; } = string.Empty;

    public BeatType BeatType     { get; set; } = BeatType.Narrative;

    /// <summary>
    /// Required for Narrative beats; must be null for Consequence beats.
    /// </summary>
    public int?    SequenceOrder { get; set; }

    public BeatApp App           { get; set; }
    public string? SenderName    { get; set; }

    /// <summary>Full beat payload — text, avatar, sound_effect, choices preview.</summary>
    public StoryBeatContent ContentJson { get; set; } = null!;

    /// <summary>Optional desktop OS side-effect triggered when this beat fires.</summary>
    public DesktopEvent?    DesktopEvent { get; set; }

    public decimal DelaySeconds  { get; set; } = 0m;

    /// <summary>True when this beat presents choices to the player.</summary>
    public bool    HasChoices    { get; set; } = false;

    // ── Consequence-beat extras ──────────────────────────────────────────────

    /// <summary>
    /// Required when BeatType = Consequence.
    /// 'start' = prepend to narrative flow; 'end' = append.
    /// </summary>
    public string? InjectPosition { get; set; }
}
