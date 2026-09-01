using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Controllers;

/// <summary>
/// Read-only assessment endpoints for player mastery data.
/// Assessment events are generated internally by gameplay services — the
/// frontend never sends raw assessment events for trusted backend actions.
/// TODO(identity): replace {playerId} route param with authenticated principal.
/// </summary>
[ApiController]
[Route("api/assessment")]
public class AssessmentController(IAssessmentService _assessment) : ControllerBase
{
    /// <summary>Returns all mastery snapshots for the specified player.</summary>
    [HttpGet("player/{playerId:int}/mastery")]
    public async Task<ActionResult<IEnumerable<ConceptMasterySnapshotDto>>> GetPlayerMastery(
        int playerId, CancellationToken ct)
    {
        var result = await _assessment.GetPlayerMasteryAsync(playerId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    /// <summary>Returns the player's weakest concepts (lowest mastery scores).</summary>
    [HttpGet("player/{playerId:int}/weakest-concepts")]
    public async Task<ActionResult<IEnumerable<ConceptMasterySnapshotDto>>> GetWeakestConcepts(
        int playerId, [FromQuery] int topN = 3, CancellationToken ct = default)
    {
        var result = await _assessment.GetWeakestConceptsAsync(playerId, topN, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }
}
