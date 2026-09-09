namespace LoopGame.Application.Dtos;

/// <summary>
/// Read / response model for a Practice Task (player or admin view).
/// </summary>
public class PracticeDto
{
    public int TaskId { get; set; }
    public int ShiftId { get; set; }
    public byte TaskOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? StarterCode { get; set; }
    public Concept ConceptTag { get; set; }

    /// <summary>SpacedRetrieval | Standard | Challenge</summary>
    public string Difficulty { get; set; } = "Standard";

    public short MaxAttempts { get; set; }
    public decimal EgpReward { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ShiftNumber { get; set; }
    public List<TestCaseDto>? TestCases { get; set; }
}
