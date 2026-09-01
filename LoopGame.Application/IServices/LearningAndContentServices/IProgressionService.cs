using LoopGame.Domain.Abstractions;
using LoopGame.Domain.Entities.Player;
using LoopGame.Domain.Enums;

namespace LoopGame.Application.IServices.LearningAndContentServices;

/// <summary>
/// Owns the PlayerShiftProgress / Gate business logic for a practice submission.
///
/// Gate rule (from SRS): the practice gate is cleared when the player achieves
/// a SINGLE Ideal or Acceptable result on any attempt.
/// (This preserves the exact behavior found in the original UpdateGateStatus.)
///
/// Does NOT call SaveAsync — the orchestrator commits the unit-of-work after
/// all staged changes (Attempt + Progress) are ready.
/// </summary>
public interface IProgressionService
{
    /// <summary>
    /// Increments GateAttempts and evaluates the gate cleared state for
    /// the given PlayerShiftProgress record.
    /// Stages any mutations to the repository but does NOT commit.
    /// </summary>
    Task<Result<GateProgressResult>> ProcessSubmissionAsync(
        PlayerShiftProgress progress,
        ChoiceTier tier,
        CancellationToken ct = default);
}
