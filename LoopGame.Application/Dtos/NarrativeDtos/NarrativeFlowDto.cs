namespace LoopGame.Application.Dtos.NarrativeDtos;

public class NarrativeFlowDto
{
    public int ShiftId { get; set; }
    public ShiftDto Shift { get; set; } = null!;
    public List<BeatDto> Beats { get; set; } = [];
}