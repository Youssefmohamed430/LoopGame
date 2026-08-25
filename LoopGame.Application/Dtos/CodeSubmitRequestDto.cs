namespace LoopGame.Application.Dtos;

public class CodeSubmitRequestDto
{
    public int TaskId { get; set; }

    public string SubmittedCode { get; set; } = string.Empty;

    public int TimeSpentSec { get; set; }

    public bool HintUsed { get; set; }
}