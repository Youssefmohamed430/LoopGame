namespace LoopGame.Application.Dtos;

/// <summary>
/// Telemetry event handed to the fire-and-forget emitter (UC-ASSESS-01).
/// NEVER persisted inside a money transaction (HARD RULE 6) — the real
/// AssessmentService will batch-insert these via its background channel worker.
/// </summary>
public record AssessmentEventDto(
    int PlayerId,
    string EventType,
    string? ConceptTag,
    string? Tier,
    string? PayloadJson,
    Guid? SessionId = null,
    DateTime? RecordedAt = null);
