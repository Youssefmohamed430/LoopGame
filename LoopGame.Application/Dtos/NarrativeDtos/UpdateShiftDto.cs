namespace LoopGame.Application.Dtos.NarrativeDtos;

/// <summary>
/// Request model for updating editable Shift metadata (admin content management).
/// All fields are optional — only non-null values are applied.
/// Caller must not use this to silently modify player runtime state.
/// </summary>
public class UpdateShiftDto
{
    public int?    ShiftNumber   { get; set; }
    public int?    ChapterNumber { get; set; }
    public string? Title         { get; set; }
    public string? Description   { get; set; }
    public bool?   IsCapstone    { get; set; }

    /// <summary>
    /// Pass null to clear the unlock gate; omit the field to leave it unchanged.
    /// Use the sentinel <see cref="ClearUnlockCondition"/> = true to explicitly remove the gate.
    /// </summary>
    public ShiftUnlockCondition? UnlockCondition   { get; set; }
    public bool                  ClearUnlockCondition { get; set; } = false;
}
