namespace LoopGame.Application.Dtos;

public class TestCaseDto
{
    public int TestCaseId { get; set; }
    public int? TaskId { get; set; }       
    public int? SideTaskId { get; set; }       
    public string? TestInput { get; set; }
    public string? ExpectedOutput { get; set; } 
    public bool IsHidden { get; set; } = false;
    public string? Description { get; set; }
}
