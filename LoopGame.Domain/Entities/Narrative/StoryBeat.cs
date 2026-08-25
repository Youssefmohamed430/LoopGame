namespace LoopGame.Domain.Entities.Narrative;

/// <summary>
/// A single narrative delivery unit within a shift.
/// BeatType = 'narrative' for ordered shift beats; 'consequence' for deferred injected beats.
/// ContentJson and DesktopEvent are stored as JSON in the database.
/// </summary>
public class StoryBeat
{
    public int       BeatId         { get; set; }
    public int       ShiftId        { get; set; }
    public string    BeatKey        { get; set; } = string.Empty;
    public BeatType  BeatType       { get; set; } = BeatType.Narrative;
    public int?      SequenceOrder  { get; set; }
    public BeatApp   App            { get; set; }
    public string?   SenderName     { get; set; }

    /// <summary>Full beat payload stored as JSON (content_json column).</summary>
    public StoryBeatContent ContentJson  { get; set; } = null!;

    /// <summary>Optional desktop side-effect stored as JSON (desktop_event column).</summary>
    public DesktopEvent?    DesktopEvent { get; set; }

    public decimal   DelaySeconds   { get; set; } = 0m;
    public bool      HasChoices     { get; set; } = false;
    public DateTime  CreatedAt      { get; set; } = DateTime.UtcNow;

    // Navigation
    public Shift                  Shift       { get; set; } = null!;
    public ICollection<Choice>    Choices     { get; set; } = [];
    public Consequence?           Consequence { get; set; }
}
