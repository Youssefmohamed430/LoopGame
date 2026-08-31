namespace LoopGame.Domain.Abstractions;

public static class SideTaskErrors
{
    public static readonly Error NoActiveTask         = new("SideTask.NoActiveTask",         "No active side task assigned to this player.");
    public static readonly Error AlreadyHasActiveTask = new("SideTask.AlreadyHasActiveTask", "Player already has an active side task.");
    public static readonly Error TaskNotFound         = new("SideTask.TaskNotFound",         "Side task not found or does not belong to this player.");
    public static readonly Error TaskExpired          = new("SideTask.TaskExpired",           "The side task deadline has passed.");
    public static readonly Error TaskAlreadyClosed    = new("SideTask.TaskAlreadyClosed",     "Task is already submitted or abandoned.");
    public static readonly Error TemplateNotFound     = new("SideTask.TemplateNotFound",      "No suitable side task template found for this player's rank.");
}
