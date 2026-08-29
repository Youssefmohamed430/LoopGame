namespace LoopGame.Application.Dtos;

public class CodeSubmitResponseDto
{
    public ChoiceTier Tier { get; set; }

    public string? TestResults { get; set; }

    public bool GateCleared { get; set; }

    //public decimal EgpEarned { get; set; }

    //public decimal? NewBalance { get; set; }

    public bool StruggleDetected { get; set; }

    public bool MaxAttemptsReached { get; set; }
}