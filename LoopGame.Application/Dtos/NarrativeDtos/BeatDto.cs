namespace LoopGame.Application.Dtos.NarrativeDtos;

public class BeatDto
{
    public int       BeatId         { get; set; }
    public string    BeatKey        { get; set; } = string.Empty;
    public BeatType  BeatType       { get; set; } = BeatType.Narrative;
    public int?      SequenceOrder  { get; set; }
    public BeatApp   App            { get; set; }
    public string?   SenderName     { get; set; }

    /// <summary>Full beat payload stored as JSON (content_json column).</summary>
    public StoryBeatContent ContentJson  { get; set; } = null!;

    /// <summary>Optional desktop side-effect stored as JSON (desktop_event column).</summary>
    public DesktopEvent?    DesktopEvent { get; set; }

    public decimal   DelaySeconds   { get; set; } = 0m;
    public bool      HasChoices     { get; set; } = false;
    public DateTime  CreatedAt      { get; set; } = DateTime.UtcNow;
    public List<ChoiceDto>? Choices { get; set; }   
}