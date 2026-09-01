namespace LoopGame.Application.IServices.EconomyAndProgressionServices;

/// <summary>
/// Fire-and-forget telemetry emitter (rule 6). Implementations must never block
/// or throw into the caller's flow; persistence happens out-of-band
/// (AssessmentService channel + background worker, §5.11).
/// </summary>
public interface IAssessmentEventEmitter
{
    void Emit(AssessmentEventDto assessmentEvent);
}
