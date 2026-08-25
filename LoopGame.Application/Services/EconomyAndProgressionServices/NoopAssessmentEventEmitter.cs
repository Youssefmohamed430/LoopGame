using LoopGame.Application.IServices.EconomyAndProgressionServices;

namespace LoopGame.Application.Services.EconomyAndProgressionServices;

/// <summary>
/// Stub implementation until the Assessment group delivers the real
/// Channel-based emitter (§5.11). Deliberately does nothing — never throws,
/// never blocks.
/// </summary>
public class NoopAssessmentEventEmitter : IAssessmentEventEmitter
{
    public void Emit(AssessmentEventDto assessmentEvent) { /* stub: no-op */ }
}
