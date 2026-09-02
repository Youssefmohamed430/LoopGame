namespace LoopGame.Domain.Entities.SideTask;

/// <summary>
/// AI-generated side task instance assigned to a specific player.
/// filled_slots contains the AI-resolved slot values as JSON.
/// </summary>
public class PlayerSideTask
{
    public int            SideTaskId           { get; set; }
    public int            PlayerId             { get; set; }
    public int            TemplateId           { get; set; }
    public int?           AiLogId              { get; set; }
    // Title and description to the task.
    public string         ResolvedTitle        { get; set; } = string.Empty;
    public string         ResolvedDescription  { get; set; } = string.Empty;

    /// <summary>JSON dictionary of slot name → resolved value.</summary>
    public string         FilledSlots          { get; set; } = "{}";

    public decimal        EgpReward            { get; set; }
    public SideTaskStatus Status               { get; set; } = SideTaskStatus.Active;
    public DateTime       AssignedAt           { get; set; } = DateTime.UtcNow;
    public DateTime?      CompletedAt          { get; set; }

    // Navigation
    public Player.Player             Player      { get; set; } = null!;
    public SideTaskTemplate          Template    { get; set; } = null!;
    public Audit.AiGenerationLog?    AiLog       { get; set; }
    public ICollection<SideTaskSubmission> Submissions { get; set; } = [];
    public ICollection<SideTaskHint>      Hints       { get; set; } = [];
    public ICollection<TestCase> TestCases { get; set; } = [];

}
