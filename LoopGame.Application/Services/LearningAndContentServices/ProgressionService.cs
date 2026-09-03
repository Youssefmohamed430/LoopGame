using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Domain.Abstractions;
using LoopGame.Domain.Entities.Player;
using LoopGame.Domain.Enums;

namespace LoopGame.Application.Services.LearningAndContentServices;

/// <summary>
/// Owns all PlayerShiftProgress / Gate mutation logic for a practice submission.
/// Extracted from PracticeService.UpdatePlayerProgress() and UpdateGateStatus().
///
/// Gate rule (from SRS Sequence Diagram, preserved exactly):
///   Ideal or Acceptable → gate cleared, shift completed.
///   Debt or Mistake     → gate not cleared, status remains GatePending.
///
/// Does NOT call SaveAsync. The orchestrator commits the UoW once, after both
/// PracticeAttempt and PlayerShiftProgress are staged.
/// </summary>
public sealed class ProgressionService(IUnitOfWork _uow) : IProgressionService
{
    public Task<Result<GateProgressResult>> ProcessSubmissionAsync(
        PlayerShiftProgress progress,
        ChoiceTier tier,
        CancellationToken ct = default)
    {
        // Always increment gate attempt counter.
        progress.GateAttempts++;

        bool isCorrect = tier == ChoiceTier.Ideal || tier == ChoiceTier.Acceptable;

        if (isCorrect && !progress.IsGateCleared)
        {
            // First passing attempt clears the gate.
            progress.IsGateCleared  = true;
            progress.GateClearedAt  = DateTime.UtcNow;
            progress.Status         = ShiftProgressStatus.Completed;
            progress.CompletedAt    = DateTime.UtcNow;
        }
        else if (!isCorrect && !progress.IsGateCleared)
        {
            progress.Status = ShiftProgressStatus.GatePending;
        }
        // If gate is already cleared, we still count the attempt but don't regress status.

        _uow.GetRepository<PlayerShiftProgress>().UpdateAsync(progress);

        return Task.FromResult(Result.Success(
            new GateProgressResult(progress.IsGateCleared, progress.GateAttempts)));
    }
}
