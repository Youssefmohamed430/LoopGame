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
public sealed class ProgressionService(IUnitOfWork _uow, IEconomyService _economyService) : IProgressionService
{
    public async Task<Result<GateProgressResult>> ProcessSubmissionAsync(
        PlayerShiftProgress progress,
        ChoiceTier tier,
        int taskid,
        CancellationToken ct = default)
    {
        // Always increment gate attempt counter.
        progress.GateAttempts++;

        bool isCorrect = tier == ChoiceTier.Ideal || tier == ChoiceTier.Acceptable;
        var tasksCompleted = _uow.GetRepository<PracticeAttempt>()
            .FindAll(pa =>
                pa.PlayerId == progress.PlayerId &&
                pa.Task.ShiftId == progress.ShiftId)
            .GroupBy(pa => pa.TaskId)
            .Select(g => new
            {
                TaskId = g.Key,
                CompletedAttempt = g.FirstOrDefault(pa => pa.IsCompleted)
            })
            .Where(x => x.CompletedAttempt != null)
            .ToList();

        if (isCorrect && !progress.IsGateCleared)
        {
            if (tasksCompleted.Count() == progress.Shift.NumberOfTasks)
            {

                // First passing attempt clears the gate.
                progress.IsGateCleared  = true;
                progress.GateClearedAt  = DateTime.UtcNow;
                progress.Status         = ShiftProgressStatus.Completed;
                progress.CompletedAt    = DateTime.UtcNow;

                await _economyService.PayShiftSalaryAsync(progress.PlayerId, progress.ShiftId, ct);
            }
        }
        else if (!isCorrect && !progress.IsGateCleared)
        {
            progress.Status = ShiftProgressStatus.GatePending;
        }
        // If gate is already cleared, we still count the attempt but don't regress status.

        await _uow.GetRepository<PlayerShiftProgress>().UpdateAsync(progress);

        return Result.Success(
            new GateProgressResult(progress.IsGateCleared, progress.GateAttempts));
    }
}
