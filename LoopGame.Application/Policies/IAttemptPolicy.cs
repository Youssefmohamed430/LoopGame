using LoopGame.Domain.Abstractions;
using LoopGame.Domain.Entities.Code;

namespace LoopGame.Application.IServices.LearningAndContentServices;

/// <summary>
/// Policy that decides whether a player may submit another attempt for a task.
/// MaxAttempts == 0 means unlimited attempts (preserved from original behavior).
/// </summary>
public interface IAttemptPolicy
{
    /// <summary>
    /// Returns failure if MaxAttempts > 0 and the player has already reached
    /// that limit for the given task. Otherwise returns success.
    /// </summary>
    Result CheckCanAttempt(int playerId, int taskId, short maxAttempts);
}
