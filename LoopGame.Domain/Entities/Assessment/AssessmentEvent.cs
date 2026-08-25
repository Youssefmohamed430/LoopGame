namespace LoopGame.Domain.Entities.Assessment;

/// <summary>
/// Central telemetry log for stealth assessment.
/// event_id is BIGINT (long) to support high-volume inserts.
/// payload is a JSON string containing event-specific telemetry.
/// </summary>
public class AssessmentEvent
{
    public long     EventId     { get; set; }
    public int      PlayerId    { get; set; }

    /// <summary>
    /// choice_submission | practice_attempt | hint_request | side_task_submission |
    /// desktop_interaction | consequence_trigger | gate_cleared | shift_completed
    /// </summary>
    public string   EventType   { get; set; } = string.Empty;

    public string?  ConceptTag  { get; set; }
    public string?  Tier        { get; set; }

    /// <summary>JSON telemetry payload (event-specific).</summary>
    public string?  Payload     { get; set; }

    public Guid?    SessionId   { get; set; }
    public DateTime RecordedAt  { get; set; } = DateTime.UtcNow;

    // Navigation
    public Player.Player Player { get; set; } = null!;
}
