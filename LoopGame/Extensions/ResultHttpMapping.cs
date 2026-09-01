namespace LoopGame.Extensions;

/// <summary>
/// Maps typed domain Error codes to HTTP status codes for our group's endpoints.
/// Auth/identity error mapping (401/403) belongs to the identity group.
/// </summary>
public static class ResultHttpMapping
{
    public static ActionResult ToActionResult(this Error error) =>
        new ObjectResult(new { error.Code, error.Description })
        {
            StatusCode = StatusCodeFor(error.Code)
        };

    public static ActionResult<T> ToActionResult<T>(this Error error) =>
        new ObjectResult(new { error.Code, error.Description })
        {
            StatusCode = StatusCodeFor(error.Code)
        };

    private static int StatusCodeFor(string code) => code switch
    {
        "Economy.InsufficientBalance" or "Shop.InsufficientBalance" => 402, // Payment Required

        "Sahm.DailyHintLimitReached"                                => 429, // Too Many Requests

        "Economy.SalaryAlreadyPaid" or
        "Shop.AlreadyOwned"                                         => 409, // Conflict

        "Economy.PlayerNotFound" or
        "Economy.PlayerEconomyNotFound" or
        "Shop.ItemNotFoundOrUnavailable"                            => 404,

        // ── Narrative not-found errors → 404 ─────────────────────────────
        "Narrative.ShiftNotFound" or
        "Narrative.BeatNotFound"                                    => 404,

        // ── Narrative conflict / dependency errors → 409 ──────────────────
        "Narrative.DuplicateShiftNumber" or
        "Narrative.DuplicateBeatKey"     or
        "Narrative.SequenceOrderConflict" or
        "Narrative.ShiftHasPlayerProgress" or
        "Narrative.ShiftHasStoryBeats"   or
        "Narrative.BeatHasActiveConsequenceQueues" or
        "Narrative.BeatHasConsequenceReference"   or
        "Narrative.BeatHasChoices"       or
        "Narrative.BeatAlreadyInShift"   or
        "Narrative.ConsequenceBeatCannotChangeShift" => 409,

        // ── Choice / player errors → 404 / 400 already covered by ChoiceErrors ─
        "Choice.PlayerNotFound" or
        "Choice.BeatNotFound"   or
        "Choice.ChoiceNotFound"                                     => 404,

        "Choice.ShiftMismatch"                                      => 409,

        // ── Assessment errors → 404 / 400 ─────────────────────────────
        "Assessment.PlayerNotFound" or
        "Assessment.ShiftNotFound"  or
        "Assessment.NoEventsFound"                                  => 404,

        "Forbidden.Access"                                          => 403,

        _ => 400 // validation errors, unknown
    };
}
