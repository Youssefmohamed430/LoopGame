using LoopGame.Application.Dtos.SideTaskDtos;

namespace LoopGame.Application.IServices.SystemAndUtilityServices;

public interface ISideTaskService
{
    /// <summary>Returns the player's currently active side task, or fails if none exists.</summary>
    Task<Result<SideTaskDto>> GetActiveTaskAsync(int playerId, CancellationToken ct = default);

    /// <summary>
    /// Runs the player's submitted code against the task's test cases,
    /// records a SideTaskSubmission, updates task status, and credits EGP earned.
    /// </summary>
    Task<Result<CodeSubmitResponseDto>> SubmitSideTaskAsync(int playerId, SideTaskSubmitRequestDto dto, CancellationToken ct = default);

    /// <summary>
    /// Marks the task as Abandoned and applies the flat EGP abandonment penalty.
    /// </summary>
    Task<Result<AbandonResultDto>> AbandonTaskAsync(int playerId, int sideTaskId, CancellationToken ct = default);

    /// <summary>
    /// Picks a suitable template, resolves reward, and inserts a new PlayerSideTask.
    /// Called internally after a gate is cleared (or manually by the player's first task).
    /// </summary>
    Task<Result> AssignNewTaskAsync(int playerId, CancellationToken ct = default);

    // ── Hints ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all pre-generated hints for the given active task.
    /// HintText is null for hints that are still locked (privacy guard).
    /// </summary>
    Task<Result<List<SideTaskHintDto>>> GetHintsAsync(int playerId, int sideTaskId, CancellationToken ct = default);

    /// <summary>
    /// Deducts EgpCost from the player's balance and marks the hint as unlocked,
    /// returning the revealed HintText and the player's new balance.
    /// Fails if: task not found / not active / hint already unlocked /
    ///           hint level not found / insufficient balance.
    /// </summary>
    Task<Result<UnlockHintResultDto>> UnlockHintAsync(int playerId, UnlockHintRequestDto dto, CancellationToken ct = default);
}
