namespace LoopGame.Domain.Abstractions;

public static class ChoiceErrors
{
    public static readonly Error InvalidId            = new("Choice.InvalidId", "Invalid ID provided.");
    public static readonly Error PlayerNotFound       = new("Choice.PlayerNotFound", "No player exists with this identifier.");
    public static readonly Error BeatNotFound         = new("Choice.BeatNotFound", "No story beat exists with this identifier.");
    public static readonly Error ShiftMismatch        = new("Choice.ShiftMismatch", "The player's current shift does not match the story beat shift.");
    public static readonly Error ChoiceNotFound       = new("Choice.ChoiceNotFound", "No choice exists with this identifier.");
    public static readonly Error EmptyChoicesList     = new("Choice.EmptyChoicesList", "The choice list cannot be empty.");
    public static readonly Error ExceedsMaxChoices    = new("Choice.ExceedsMaxChoices", "A story beat cannot have more than 4 choices.");
    public static readonly Error InvalidChoiceIndex   = new("Choice.InvalidChoiceIndex", "Choice index must be between 1 and 4.");
    public static readonly Error DuplicateChoiceIndex = new("Choice.DuplicateChoiceIndex", "Duplicate choice index found for the story beat.");
    public static readonly Error InvalidChoiceText    = new("Choice.InvalidChoiceText", "Choice text cannot be empty.");
    public static readonly Error InvalidConsequence   = new("Choice.InvalidConsequence", "The specified consequence does not exist.");
}
