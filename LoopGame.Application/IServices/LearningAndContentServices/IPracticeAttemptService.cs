using LoopGame.Domain.Abstractions;
using LoopGame.Domain.Entities.Code;
using LoopGame.Domain.Enums;
using LoopGame.Domain.ValueObjects;

namespace LoopGame.Application.IServices.LearningAndContentServices;

/// <summary>
/// Records (persists) a single PracticeAttempt for a code submission.
/// Does NOT call SaveAsync — the caller (orchestrator) commits the unit of work
/// together with PlayerShiftProgress changes in one logical transaction.
/// </summary>
public interface IPracticeAttemptService
{
    /// <summary>
    /// Stages a new <see cref="PracticeAttempt"/> in the repository.
    /// Returns the new AttemptId.
    /// </summary>
    Task<int> RecordAttemptAsync(
        int playerId,
        ChoiceTier tier,
        IReadOnlyList<TestCaseResult> testResults,
        CodeSubmitRequestDto  code,
        CancellationToken ct = default);
}
