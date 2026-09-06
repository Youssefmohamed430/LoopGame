using LoopGame.Application.Dtos;
using LoopGame.Application.Dtos.SideTaskDtos;
using LoopGame.Application.IServices.SystemAndUtilityServices;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Controllers;

/// <summary>
/// Side-task endpoints:
/// - Retrieve active side task
/// - Submit code solution for grading and EGP reward
/// - Abandon active side task with penalty
/// - Retrieve and unlock pre-generated progressive hints (UC-SIDETASK-HINT-01/02)
/// - Optional manual assignment fallback
/// </summary>
[ApiController]
[Route("api/sidetask")]
public class SideTaskController(ISideTaskService _sideTask) : ControllerBase
{
    /// <summary>
    /// Returns the player's currently active side task.
    /// </summary>
    [HttpGet("{playerId:int}/active")]
    public async Task<ActionResult<SideTaskDto>> GetActiveTask(int playerId, CancellationToken ct)
    {
        var result = await _sideTask.GetActiveTaskAsync(playerId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    /// <summary>
    /// Submits player code solution for their active side task.
    /// Evaluates against test cases, calculates choice tier, credits EGP reward,
    /// and activates the next queued task if available.
    /// </summary>
    [HttpPost("{playerId:int}/submit")]
    public async Task<ActionResult<CodeSubmitResponseDto>> SubmitSideTask(
        int playerId, [FromBody] SideTaskSubmitRequestDto request, CancellationToken ct)
    {
        var result = await _sideTask.SubmitSideTaskAsync(playerId, request, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    /// <summary>
    /// Abandons the specified active side task, applies the EGP abandonment penalty (-100 EGP),
    /// and activates the next queued task if available.
    /// </summary>
    [HttpPost("{playerId:int}/{sideTaskId:int}/abandon")]
    public async Task<ActionResult<AbandonResultDto>> AbandonTask(
        int playerId, int sideTaskId, CancellationToken ct)
    {
        var result = await _sideTask.AbandonTaskAsync(playerId, sideTaskId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    /// <summary>
    /// Manually assigns a new side task for the player from eligible templates.
    /// </summary>
    [Authorize("Admin")]
    [HttpPost("{playerId:int}/assign")]
    public async Task<ActionResult> AssignTask(int playerId, CancellationToken ct)
    {
        var result = await _sideTask.AssignNewTaskAsync(playerId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok();
    }

    /// <summary>
    /// Returns all hints for the specified active side task.
    /// Locked hints are returned without HintText (null) — text is revealed only after unlock.
    /// </summary>
    [HttpGet("{playerId:int}/{sideTaskId:int}/hints")]
    public async Task<ActionResult<List<SideTaskHintDto>>> GetHints(
        int playerId, int sideTaskId, CancellationToken ct)
    {
        var result = await _sideTask.GetHintsAsync(playerId, sideTaskId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    /// <summary>
    /// Unlocks the requested hint level for the player's active side task.
    /// Deducts EgpCost (if > 0) and returns the revealed hint text + new balance.
    /// </summary>
    [HttpPost("{playerId:int}/hints/unlock")]
    public async Task<ActionResult<UnlockHintResultDto>> UnlockHint(
        int playerId, [FromBody] UnlockHintRequestDto request, CancellationToken ct)
    {
        var result = await _sideTask.UnlockHintAsync(playerId, request, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }
}
