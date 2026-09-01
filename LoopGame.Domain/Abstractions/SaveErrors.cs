namespace LoopGame.Domain.Abstractions;

public static class SaveErrors
{
    public static readonly Error InvalidSlot    = new("Save.InvalidSlot",    "Slot number must be 1, 2, or 3.");
    public static readonly Error PlayerNotFound = new("Save.PlayerNotFound", "No player exists with this identifier.");
    public static readonly Error SaveNotFound   = new("Save.SaveNotFound",   "No save data found for this slot.");
}
