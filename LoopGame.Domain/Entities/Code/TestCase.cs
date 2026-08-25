namespace LoopGame.Domain.Entities.Code;

/// <summary>
/// Test inputs and expected outputs for evaluating student code submissions.
/// Belongs to EITHER a PracticeTask OR a SideTaskTemplate — not both.
/// This is enforced by the CHK_TestCase_Parent CHECK constraint.
/// </summary>
public class TestCase
{
    public int     TestCaseId      { get; set; }
    public int?    TaskId          { get; set; }       // FK → PracticeTask (nullable)
    public int?    TemplateId      { get; set; }       // FK → SideTaskTemplate (nullable)
    public string  TestInput       { get; set; } = string.Empty;
    public string  ExpectedOutput  { get; set; } = string.Empty;
    public bool    IsHidden        { get; set; } = false;
    public string? Description     { get; set; }

    // Navigation
    public PracticeTask?       Task     { get; set; }
    public SideTaskTemplate?   Template { get; set; }
}
