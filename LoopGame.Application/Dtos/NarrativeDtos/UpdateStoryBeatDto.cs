namespace LoopGame.Application.Dtos.NarrativeDtos;

/// <summary>
/// Request model for updating an existing StoryBeat (admin content management).
/// All fields are optional — only non-null values are applied.
///
/// WARNING: changing ShiftId or SequenceOrder affects narrative flow.
/// The service validates all ordering constraints before applying the change.
/// </summary>
public class UpdateStoryBeatDto
{
    /// <summary>Move this beat to a different shift. Validated for existence.</summary>
    public int?     ShiftId       { get; set; }

    public BeatType? BeatType     { get; set; }

    /// <summary>
    /// For Narrative beats only. Must be null when changing to Consequence.
    /// </summary>
    public int?     SequenceOrder { get; set; }

    public BeatApp? App           { get; set; }
    public string?  SenderName    { get; set; }

    public StoryBeatContent? ContentJson  { get; set; }
    public DesktopEvent?     DesktopEvent { get; set; }

    public decimal? DelaySeconds  { get; set; }
    public bool?    HasChoices    { get; set; }

    // ── Consequence-beat extras ──────────────────────────────────────────────

    /// <summary>
    /// Applicable only when BeatType = Consequence.
    /// Explicitly set to update; leave null to retain the current value.
    /// </summary>
    public string?  InjectPosition { get; set; }

    /// <summary>
    /// Set to true when SequenceOrder is being changed, to indicate the caller
    /// accepts responsibility for ordering side-effects (the service still validates).
    /// </summary>
    public bool ReorderSiblings { get; set; } = false;
}
