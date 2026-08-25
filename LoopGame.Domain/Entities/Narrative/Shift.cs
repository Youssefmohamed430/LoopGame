namespace LoopGame.Domain.Entities.Narrative;

/// <summary>
/// Represents a narrative workday / chapter shift.
/// UnlockCondition is stored as JSON in the database and deserialized as ShiftUnlockCondition.
/// </summary>
public class Shift
{
    public int       ShiftId         { get; set; }
    public int       ShiftNumber     { get; set; }
    public int       ChapterNumber   { get; set; }
    public string    Title           { get; set; } = string.Empty;
    public string?   Description     { get; set; }
    public bool      IsCapstone      { get; set; } = false;

    /// <summary>Stored as JSON in the DB. Null means no unlock gate.</summary>
    public ShiftUnlockCondition? UnlockCondition { get; set; }

    public DateTime  CreatedAt       { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<StoryBeat>                          StoryBeats              { get; set; } = [];
    public ICollection<Code.PracticeTask>                  PracticeTasks           { get; set; } = [];
    public ICollection<Player.PlayerShiftProgress>         ShiftProgresses         { get; set; } = [];
    public ICollection<Assessment.ConceptMasterySnapshot>  MasterySnapshots        { get; set; } = [];
}
