namespace LoopGame.Domain.Abstractions;

public static class AdminErrors
{
    public static readonly Error ShiftNotFound      = new("Admin.ShiftNotFound",      "Shift not found.");
    public static readonly Error TaskNotFound       = new("Admin.TaskNotFound",       "Practice task not found.");
    public static readonly Error TemplateNotFound   = new("Admin.TemplateNotFound",   "Side task template not found.");
    public static readonly Error DuplicateTaskOrder = new("Admin.DuplicateTaskOrder", "A practice task with this order already exists in this shift.");
    public static readonly Error PlayerNotFound     = new("Admin.PlayerNotFound",     "Player not found.");
    public static readonly Error InvalidFileFormat  = new("Admin.InvalidFileFormat",  "The uploaded file format is invalid. Expected a UTF-8 CSV file.");
    
}
