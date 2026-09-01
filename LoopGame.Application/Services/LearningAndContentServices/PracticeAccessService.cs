using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Domain.Abstractions;
using LoopGame.Domain.Entities.Code;
using LoopGame.Domain.Entities.Player;

namespace LoopGame.Application.Services.LearningAndContentServices;

/// <summary>
/// Validates player/task access and returns a pre-loaded context for the
/// submission flow. Extracted from PracticeService.CheckAccess().
///
/// Also loads the PlayerShiftProgress record so downstream services can work
/// with the correct (PlayerId, ShiftId) pair rather than guessing.
/// </summary>
public sealed class PracticeAccessService(IUnitOfWork _uow) : IPracticeAccessService
{
    public async Task<Result<PracticeAccessContext>> ValidateAccessAsync(
        int playerId, int taskId, CancellationToken ct = default)
    {
        // Load player with the navigation properties required for the access checks.
        var player = await _uow.GetRepository<Player>()
            .FindAsync(
                p => p.PlayerId == playerId,
                new[] { "CurrentShift.PracticeTasks", "ShiftProgresses" });

        if (player is null)
            return Result.Failure<PracticeAccessContext>(PracticeErrors.PlayerNotFound);

        if (player.CurrentShift is null || player.CurrentShiftId is null)
            return Result.Failure<PracticeAccessContext>(PracticeErrors.NoActiveShift);

        if (!player.CurrentShift.PracticeTasks.Any(t => t.TaskId == taskId))
            return Result.Failure<PracticeAccessContext>(PracticeErrors.TaskNotInShift);

        int shiftId = player.CurrentShiftId.Value;

        // Use (PlayerId + ShiftId) — never just PlayerId — to find progress.
        var progress = player.ShiftProgresses
            .FirstOrDefault(p => p.ShiftId == shiftId);

        if (progress is null)
            return Result.Failure<PracticeAccessContext>(PracticeErrors.ProgressNotFound);

        return Result.Success(new PracticeAccessContext(player, progress, shiftId));
    }
}
