using Microsoft.EntityFrameworkCore.Metadata;

namespace LoopGame.Application.Dtos;

public class PracticeDto
{
    public int? TaskId { get; set; }
    public int? ShiftId { get; set; }
    public byte? TaskOrder { get; set; }
    public string? Title { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public string? StarterCode { get; set; }
    public string? ConceptTag { get; set; } = string.Empty;

    /// <summary>SpacedRetrieval | Standard | Challenge</summary>
    public string? Difficulty { get; set; } = "Standard";

    public short? MaxAttempts { get; set; } = 0;
    public decimal? EgpReward { get; set; } = 0m;
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public int? ShiftNumber { get; set; }
    public List<TestCaseDto>? TestCases { get; set; }
}
