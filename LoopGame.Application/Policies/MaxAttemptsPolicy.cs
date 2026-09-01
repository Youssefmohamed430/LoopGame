using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Domain.Abstractions;
using LoopGame.Domain.Entities.Code;

namespace LoopGame.Application.Services.LearningAndContentServices;

/// <summary>
/// Checks whether the player has exhausted the allowed number of attempts.
/// MaxAttempts == 0 means unlimited (preserved from the original ValidateMaxAttempts).
/// </summary>
public sealed class MaxAttemptsPolicy(IUnitOfWork _uow) : IAttemptPolicy
{
    public Result CheckCanAttempt(int playerId, int taskId, short maxAttempts)
    {
        // 0 == unlimited
        if (maxAttempts == 0)
            return Result.Success();

        var count = _uow.GetRepository<PracticeAttempt>()
            .FindAll(a => a.PlayerId == playerId && a.TaskId == taskId)
            .Count();

        if (count >= maxAttempts)
            return Result.Failure(PracticeErrors.MaxAttemptsReached);

        return Result.Success();
    }
}
