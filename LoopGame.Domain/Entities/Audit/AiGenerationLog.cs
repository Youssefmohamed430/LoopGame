namespace LoopGame.Domain.Entities.Audit;

/// <summary>
/// Audit log of every call made to an LLM API for generating side-task slot values.
/// Rows expire after 2 years (expires_at). Cleaned up by AiLogCleanupJob.
/// </summary>
public class AiGenerationLog
{
    public int      LogId              { get; set; }
    public int      PlayerId           { get; set; }
    public int      TemplateId         { get; set; }
    public string   ModelName          { get; set; } = string.Empty;
    public string   PromptText         { get; set; } = string.Empty;
    public string?  RawResponse        { get; set; }

    /// <summary>JSON dictionary of resolved slot values.</summary>
    public string?  ParsedSlots        { get; set; }

    public bool     IsValid            { get; set; } = false;
    public string?  ValidationError    { get; set; }
    public int      LatencyMs          { get; set; } = 0;
    public decimal? EstimatedCostUsd   { get; set; } // DECIMAL(8,6)
    public DateTime CreatedAt          { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt          { get; set; } = DateTime.UtcNow.AddYears(2);

    // Navigation
    public Player.Player           Player   { get; set; } = null!;
    public SideTaskTemplate        Template { get; set; } = null!;
    public ICollection<PlayerSideTask> PlayerSideTasks { get; set; } = [];
}
