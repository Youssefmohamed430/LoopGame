using LoopGame.Extensions;

namespace LoopGame.Controllers;

/// <summary>
/// Player Narrative and Choice Endpoints — Shift Start, Flow Loading, and Choice Submission.
///
/// All endpoints here are for **Player** role use during active gameplay shifts.
///
/// Authorization: TODO — once the RBAC pipeline lands, gate these endpoints
/// with [Authorize(Roles = "Player")] (or the project's equivalent policy).
///
/// TODO(identity): replace {playerId} route param with authenticated principal
/// once the auth pipeline lands.
/// </summary>
[ApiController]
[Route("api/narrative")]
public class NarrativeController(
    INarrativeService _narrative,
    IChoiceService _choice) : ControllerBase
{
    /// <summary>
    /// Starts a shift for a player and loads the full narrative flow
    /// (merging standard narrative beats with pending consequence beats).
    /// </summary>
    [HttpPost("{playerId:int}/shifts/{shiftId:int}/start")]
    public async Task<ActionResult<NarrativeFlowDto>> StartShift(int playerId, int shiftId)
        => await Handle(_narrative.StartShift(playerId, shiftId));

    /// <summary>
    /// Retrieves all choices available to the player for a specific story beat.
    /// </summary>
    [HttpGet("{playerId:int}/beats/{beatId:int}/choices")]
    public async Task<ActionResult<List<ChoiceDto>>> GetChoices(int playerId, int beatId)
        => await Handle(_choice.GetChoices(beatId, playerId));

    /// <summary>
    /// Submits a player's choice for a story beat, queuing consequences and updating shift progress.
    /// </summary>
    [HttpPost("{playerId:int}/choices/{choiceId:int}/submit")]
    public async Task<ActionResult<ChoiceDto>> SubmitChoice(int playerId, int choiceId)
        => await Handle(_choice.SubmitChoice(choiceId, playerId));

    private async Task<ActionResult<T>> Handle<T>(Task<Result<T>> operation)
    {
        var result = await operation;
        if (result.IsFailure)
            return result.Error.ToActionResult<T>();
        return Ok(result.Value);
    }
}
