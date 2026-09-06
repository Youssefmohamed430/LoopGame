namespace LoopGame.Domain.Abstractions;

public static class SideTaskErrors
{
    public static readonly Error NoActiveTask         = new("SideTask.NoActiveTask",         "No active side task assigned to this player.");
    public static readonly Error AlreadyHasActiveTask = new("SideTask.AlreadyHasActiveTask", "Player already has an active side task.");
    public static readonly Error TaskNotFound         = new("SideTask.TaskNotFound",         "Side task not found or does not belong to this player.");
    public static readonly Error TaskExpired          = new("SideTask.TaskExpired",           "The side task deadline has passed.");
    public static readonly Error TaskAlreadyClosed    = new("SideTask.TaskAlreadyClosed",     "Task is already submitted or abandoned.");
    public static readonly Error TemplateNotFound     = new("SideTask.TemplateNotFound",      "No suitable side task template found for this player's rank.");
    public static readonly Error NoWeakConcepts        = new("SideTask.NoWeakConcepts",        "Player has no weak concepts to generate tasks for.");
    public static readonly Error NoSheetForConcept     = new("SideTask.NoSheetForConcept",     "No question sheet file found for the requested concept.");
    public static readonly Error SheetFailed           = new("SideTask.SheetFailed",           "Sheet file for this concept is in Failed status.");
    public static readonly Error NoTemplateForConcept  = new("SideTask.NoTemplateForConcept",  "No reference scenario template found for the requested concept.");
    public static readonly Error AiCallFailed          = new("SideTask.AiCallFailed",          "External AI service call failed.");
    public static readonly Error AllTasksInvalid       = new("SideTask.AllTasksInvalid",       "All AI-generated tasks failed validation after max retries.");

    // ── Hint errors ──────────────────────────────────────────────────────────
    public static readonly Error HintNotFound        = new("SideTask.HintNotFound",        "Hint not found for this task and level.");
    public static readonly Error HintAlreadyUnlocked = new("SideTask.HintAlreadyUnlocked", "This hint is already unlocked.");
}
