namespace LoopGame.Application.Dtos.NarrativeDtos;

public class ShiftDto
{
    public int ShiftId { get; set; }
    public int       ShiftNumber     { get; set; }
    public int       ChapterNumber   { get; set; }
    public string    Title           { get; set; } = string.Empty;
    public string?   Description     { get; set; }
    public bool      IsCapstone      { get; set; } = false;
}