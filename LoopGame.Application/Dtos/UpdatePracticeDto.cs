namespace LoopGame.Application.Dtos;

/// <summary>
/// Request model for updating editable Practice Task fields (admin content management).
/// All fields are optional — only non-null / non-whitespace values are applied.
/// </summary>
public class UpdatePracticeDto
{
    public byte? TaskOrder { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? StarterCode { get; set; }
    public string? ConceptTag { get; set; }

    /// <summary>SpacedRetrieval | Standard | Challenge</summary>
    public string? Difficulty { get; set; }

    public short? MaxAttempts { get; set; }
    public decimal? EgpReward { get; set; }
}
