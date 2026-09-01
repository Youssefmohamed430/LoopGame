using LoopGame.Domain.Abstractions;

namespace LoopGame.Application.IServices.LearningAndContentServices;

/// <summary>
/// Validates that a player may access a given practice task, and returns the
/// pre-loaded context (Player + ShiftProgress) needed by the submission flow.
/// Encapsulates access guard logic that was previously inlined in PracticeService.
/// </summary>
public interface IPracticeAccessService
{
    /// <summary>
    /// Verifies:
    ///   1. Player exists.
    ///   2. Player has an active CurrentShift.
    ///   3. The requested PracticeTask belongs to that shift.
    ///   4. A PlayerShiftProgress record exists for (PlayerId, ShiftId).
    ///
    /// Returns a <see cref="PracticeAccessContext"/> on success, or a failure Result.
    /// </summary>
    Task<Result<PracticeAccessContext>> ValidateAccessAsync(int playerId, int taskId,
        CancellationToken ct = default);
}
