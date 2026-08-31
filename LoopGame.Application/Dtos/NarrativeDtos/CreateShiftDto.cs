namespace LoopGame.Application.Dtos.NarrativeDtos;

/// <summary>
/// Request model for creating a new Shift (admin content management).
/// Does NOT create any player progress records.
/// </summary>
public class CreateShiftDto
{
    public int    ShiftNumber   { get; set; }
    public int    ChapterNumber { get; set; }
    public string Title         { get; set; } = string.Empty;
    public string? Description  { get; set; }
    public bool   IsCapstone    { get; set; } = false;

    /// <summary>
    /// Optional unlock gate stored as JSON.
    /// Null = no gate — shift is freely accessible.
    /// </summary>
    public ShiftUnlockCondition? UnlockCondition { get; set; }
}
