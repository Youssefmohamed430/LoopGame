namespace LoopGame.Application.Dtos;

public class TestCaseDto
{
    public int TestCaseId { get; set; }
    public int? TaskId { get; set; }       
    public int? TemplateId { get; set; }       
    public string? TestInput { get; set; } = string.Empty;
    public string? ExpectedOutput { get; set; } = string.Empty;
    public bool IsHidden { get; set; } = false;
    public string? Description { get; set; }
}
