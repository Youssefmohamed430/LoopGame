namespace LoopGame.Application.Dtos;

/// <summary>
/// Request model for creating a new Practice Task (admin content management).
/// </summary>
public class CreatePracticeDto
{
    public int ShiftId { get; set; }
    public int TaskOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? StarterCode { get; set; }
    public string ConceptTag { get; set; } = string.Empty;

    /// <summary>SpacedRetrieval | Standard | Challenge</summary>
    public string Difficulty { get; set; } = "Standard";

    public int MaxAttempts { get; set; } = 0;
    public decimal EgpReward { get; set; } = 0m;
    public List<TestCaseDto>? TestCases { get; set; }
}
