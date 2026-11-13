namespace LoopGame.Domain.Entities.Audit;

/// <summary>
/// Audit log of AI narrative generation calls (ImmediateBranch, DeferredConsequence, Repair).
/// Stores operational metrics and redacted diagnostic failure reasons only;
/// never stores raw prompts or player PII. Rows are subject to a 90-day retention pruning policy.
/// </summary>
public class NarrativeGenerationLog
{
    public const int MaxOperationTypeLength = 50;
    public const int MaxModelNameLength = 100;
    public const int MaxFailureReasonLength = 500;
    public const int RetentionDays = 90;

    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int ShiftId { get; set; }

    /// <summary>Operation category, e.g. ImmediateBranch, DeferredConsequence, Repair.</summary>
    public string OperationType { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;
    public int ResponseTimeMs { get; set; }
    public bool ValidationPassed { get; set; }
    public bool RepairAttempted { get; set; }
    public bool? RepairSucceeded { get; set; }
    public bool UsedFallback { get; set; }

    /// <summary>Redacted diagnostics only (max 500 chars). Never store PII or raw prompts.</summary>
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Player.Player Player { get; set; } = null!;
    public Shift Shift { get; set; } = null!;
    public StoryBeat? StoryBeat { get; set; }

    /// <summary>
    /// Stores a single-line, length-capped diagnostic failure reason.
    /// Callers must pass only redacted operational diagnostics (no PII or raw prompts).
    /// </summary>
    public void SetFailureReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            FailureReason = null;
            return;
        }

        var sanitized = reason.Replace('\r', ' ').Replace('\n', ' ').Trim();
        FailureReason = sanitized.Length <= MaxFailureReasonLength
            ? sanitized
            : sanitized[..MaxFailureReasonLength];
    }
}
