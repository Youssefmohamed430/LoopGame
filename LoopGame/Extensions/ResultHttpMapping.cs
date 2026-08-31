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
        "Shop.ItemNotFoundOrUnavailable" or
        "SideTask.TaskNotFound" or
        "SideTask.NoActiveTask" or
        "Save.SaveNotFound" or
        "Admin.ShiftNotFound" or
        "Admin.TaskNotFound" or
        "Admin.PlayerNotFound" or
        "Admin.TemplateNotFound"                                    => 404,

        "SideTask.TaskExpired"                                      => 410, // Gone

        "SideTask.TaskAlreadyClosed" or
        "Admin.DuplicateTaskOrder"                                  => 409, // Conflict

        "Admin.InvalidFileFormat" or
        "Save.InvalidSlot"                                          => 400,

        _ => 400 // default fallback
    };
}
