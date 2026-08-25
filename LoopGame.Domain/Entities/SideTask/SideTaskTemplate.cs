namespace LoopGame.Domain.Entities.SideTask;

/// <summary>
/// Skeleton blueprint used by the AI pipeline for dynamic side-task generation.
/// slots_schema is a JSON definition of the variable slots the AI must fill.
/// </summary>
public class SideTaskTemplate
{
    public int        TemplateId            { get; set; }
    public string     TemplateKey           { get; set; } = string.Empty;
    public string     ConceptTag            { get; set; } = string.Empty;
    public PlayerRank RankRequired          { get; set; } = PlayerRank.Intern;
    public string     TitleTemplate         { get; set; } = string.Empty;
    public string     DescriptionTemplate   { get; set; } = string.Empty;

    /// <summary>JSON slot definition schema.</summary>
    public string     SlotsSchema           { get; set; } = "{}";

    public decimal    EgpMin                { get; set; } = 500m;
    public decimal    EgpMax                { get; set; } = 3_000m;
    public bool       IsActive              { get; set; } = true;
    public DateTime   CreatedAt             { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Code.TestCase>           TestCases        { get; set; } = [];
    public ICollection<PlayerSideTask>           PlayerSideTasks  { get; set; } = [];
    public ICollection<Audit.AiGenerationLog>    AiGenerationLogs { get; set; } = [];
}
