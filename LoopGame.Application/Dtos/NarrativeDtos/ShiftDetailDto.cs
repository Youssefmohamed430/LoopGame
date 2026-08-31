namespace LoopGame.Application.Dtos.NarrativeDtos;

/// <summary>
/// Full Shift response for admin content management, including its beats and unlock condition.
/// </summary>
public class ShiftDetailDto
{
    public int    ShiftId       { get; set; }
    public int    ShiftNumber   { get; set; }
    public int    ChapterNumber { get; set; }
    public string Title         { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public bool   IsCapstone    { get; set; }

    /// <summary>Null means no unlock gate.</summary>
    public ShiftUnlockCondition? UnlockCondition { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Ordered narrative beats belonging to this shift (consequence beats excluded).</summary>
    public List<BeatDto> NarrativeBeats { get; set; } = [];

    /// <summary>Consequence beats belonging to this shift (unordered — injected at runtime).</summary>
    public List<BeatDto> ConsequenceBeats { get; set; } = [];
}
