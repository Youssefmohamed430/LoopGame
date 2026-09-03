namespace LoopGame.Application.IServices.LearningAndContentServices;

/// <summary>
/// Result returned by <see cref="IProgressionService.ProcessSubmissionAsync"/>.
/// Carries the gate outcome back to the submission orchestrator.
/// </summary>
public sealed record GateProgressResult(
    bool GateCleared,
    short GateAttempts);
