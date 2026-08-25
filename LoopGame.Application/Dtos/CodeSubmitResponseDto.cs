namespace LoopGame.Application.Dtos;

public class CodeSubmitResponseDto
{
    public string Tier { get; set; } = string.Empty;

    public List<TestCaseResultDto> TestResults { get; set; } = [];

    public bool GateCleared { get; set; }

    public decimal EgpEarned { get; set; }

    public decimal? NewBalance { get; set; }

    public bool StruggleDetected { get; set; }

    public bool MaxAttemptsReached { get; set; }
}