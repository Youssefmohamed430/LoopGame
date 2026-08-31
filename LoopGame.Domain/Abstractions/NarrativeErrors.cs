namespace LoopGame.Domain.Abstractions;

/// <summary>
/// Domain errors for all Narrative content-management and runtime operations.
/// Follows the same static Error pattern as ChoiceErrors.
/// </summary>
public static class NarrativeErrors
{
    // ── Shift errors ─────────────────────────────────────────────────────────
    public static readonly Error ShiftNotFound =
        new("Narrative.ShiftNotFound", "No shift exists with the given identifier.");

    public static readonly Error ShiftTitleRequired =
        new("Narrative.ShiftTitleRequired", "Shift title is required and cannot be empty.");

    public static readonly Error DuplicateShiftNumber =
        new("Narrative.DuplicateShiftNumber",
            "A shift with this chapter number and shift number already exists.");

    public static readonly Error ShiftHasPlayerProgress =
        new("Narrative.ShiftHasPlayerProgress",
            "Cannot delete a shift that has player progress records. Historical data must be preserved.");

    public static readonly Error ShiftHasStoryBeats =
        new("Narrative.ShiftHasStoryBeats",
            "Cannot delete a shift that still has story beats assigned to it. Reassign or delete the beats first.");

    public static readonly Error InvalidChapterNumber =
        new("Narrative.InvalidChapterNumber", "Chapter number must be greater than zero.");

    public static readonly Error InvalidShiftNumber =
        new("Narrative.InvalidShiftNumber", "Shift number must be greater than zero.");

    // ── StoryBeat errors ─────────────────────────────────────────────────────
    public static readonly Error BeatNotFound =
        new("Narrative.BeatNotFound", "No story beat exists with the given identifier.");

    public static readonly Error BeatKeyRequired =
        new("Narrative.BeatKeyRequired", "Beat key is required and must be unique.");

    public static readonly Error DuplicateBeatKey =
        new("Narrative.DuplicateBeatKey", "A story beat with this key already exists.");

    public static readonly Error ContentRequired =
        new("Narrative.ContentRequired", "Beat content (ContentJson) is required.");

    public static readonly Error ContentTextRequired =
        new("Narrative.ContentTextRequired", "Beat content must include a non-empty text field.");

    public static readonly Error SequenceOrderRequiredForNarrativeBeat =
        new("Narrative.SequenceOrderRequired",
            "Narrative beats must have a sequence order. Consequence beats must not.");

    public static readonly Error SequenceOrderConflict =
        new("Narrative.SequenceOrderConflict",
            "Another narrative beat already occupies this sequence order in the target shift.");

    public static readonly Error InvalidInjectPosition =
        new("Narrative.InvalidInjectPosition",
            "InjectPosition must be 'start' or 'end'.");

    public static readonly Error BeatHasActiveConsequenceQueues =
        new("Narrative.BeatHasActiveConsequenceQueues",
            "Cannot delete a beat that has pending or active consequence queue entries. Historical player data must be preserved.");

    public static readonly Error BeatHasChoices =
        new("Narrative.BeatHasChoices",
            "Cannot delete a beat that has active choices. Delete the choices first or reassign them.");

    public static readonly Error BeatHasConsequenceReference =
        new("Narrative.BeatHasConsequenceReference",
            "Cannot delete a consequence beat while it is referenced by a Consequence with active queue entries.");

    // ── Assign / move beat errors ─────────────────────────────────────────────
    public static readonly Error BeatAlreadyInShift =
        new("Narrative.BeatAlreadyInShift", "The beat is already assigned to this shift.");

    public static readonly Error ConsequenceBeatCannotChangeShift =
        new("Narrative.ConsequenceBeatCannotChangeShift",
            "A consequence beat's shift is derived from its content. Moving it between shifts is not supported while the consequence is active.");
}
