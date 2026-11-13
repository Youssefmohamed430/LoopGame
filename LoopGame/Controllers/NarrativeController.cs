using System.Security.Claims;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace LoopGame.Controllers;

/// <summary>
/// Player Narrative and Choice Endpoints — Shift Start, Flow Loading, and Choice Submission.
///
/// All endpoints here are for **Player** role use during active gameplay shifts.
/// </summary>
[ApiController]
[Authorize]
[Route("api/narrative")]
public class NarrativeController(
    INarrativeService _narrative,
    IChoiceService _choice) : ControllerBase
{
    /// <summary>
    /// Starts a shift for a player and loads the full narrative flow
    /// (merging standard narrative beats with pending consequence beats).
    /// </summary>
    [HttpPost("{playerId:int}/shifts/start")]
    public async Task<ActionResult<NarrativeFlowDto>> StartShift(int playerId)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        return await Handle(_narrative.StartShift(playerId));
    }

    [HttpPost("{playerId:int}/shifts/{beatId:int}/save")]
    public async Task<ActionResult<NarrativeFlowDto>> Save(int playerId, int beatId)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        return await Handle(_narrative.Save(playerId, beatId));
    }

    [HttpPost("{playerId:int}/shifts/end")]
    public async Task<ActionResult<object>> EndShift(int playerId)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        return await Handle(_narrative.EndShift(playerId));
    }

    /// <summary>
    /// Retrieves all choices available to the player for a specific story beat.
    /// </summary>
    [HttpGet("{playerId:int}/beats/{beatId:int}/choices")]
    public async Task<ActionResult<List<ChoiceDto>>> GetChoices(int playerId, int beatId)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        return await Handle(_choice.GetChoices(beatId, playerId));
    }

    /// <summary>
    /// Submits a player's choice for a story beat, queuing consequences and updating shift progress.
    /// </summary>
    [HttpPost("{playerId:int}/choices/{choiceId:int}/submit")]
    public async Task<ActionResult<ChoiceDto>> SubmitChoice(int playerId, int choiceId)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        return await Handle(_choice.SubmitChoice(choiceId, playerId));
    }

    private bool IsAuthorizedForPlayer(int playerId)
    {
        if (User?.Identity?.IsAuthenticated != true)
            return false;

        if (User.IsInRole("Admin") || User.IsInRole("admin") || User.IsInRole("super_admin"))
            return true;

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out var authId) && authId == playerId;
    }

    private async Task<ActionResult<T>> Handle<T>(Task<Result<T>> operation)
    {
        var result = await operation;
        if (result.IsFailure)
            return result.Error.ToActionResult<T>();
        return Ok(result.Value);
    }
}
