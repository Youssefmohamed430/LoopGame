using System;
namespace LoopGame.Application.Dtos;

public class CreateChoiceDto
{
    public int BeatId { get; set; }
    public byte ChoiceIndex { get; set; }
    public string ChoiceText { get; set; } = string.Empty;
    public ChoiceTier Tier { get; set; }
    public int? ConsequenceId { get; set; }
    public string? ImmediateFeedback { get; set; }
}
