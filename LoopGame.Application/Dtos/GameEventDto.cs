namespace LoopGame.Application.Dtos;

/// <summary>
/// In-process telemetry and domain game event envelope.
/// </summary>
public record GameEventDto(
    int PlayerId,
    string EventType,
    string? ConceptTag,
    string? Tier,
    string? PayloadJson,
    Guid? SessionId = null,
    DateTime? RecordedAt = null)
{
    /// <summary>
    /// Converts to the internal AssessmentEventDto format for consumption by AssessmentService.
    /// </summary>
    public AssessmentEventDto ToAssessmentDto() =>
        new(PlayerId, EventType, ConceptTag, Tier, PayloadJson, SessionId, RecordedAt);
}
