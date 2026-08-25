using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Controllers;

/// <summary>
/// Sahm AI assistant endpoints: hint requests (limit-enforced, lazy daily
/// reset) and subscription status (UC-SAHM-02/03/04/07).
/// TODO(identity): replace {playerId} route param with authenticated principal.
/// </summary>
[ApiController]
[Route("api/sahm")]
public class SahmController(ISahmService _sahm) : ControllerBase
{
    [HttpGet("{playerId:int}/status")]
    public async Task<ActionResult<SahmStatusDto>> GetStatus(int playerId, CancellationToken ct)
    {
        var result = await _sahm.GetStatusAsync(playerId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    [HttpPost("{playerId:int}/hint")]
    public async Task<ActionResult<HintResponseDto>> RequestHint(
        int playerId, [FromBody] HintRequestDto request, CancellationToken ct)
    {
        var result = await _sahm.RequestHintAsync(playerId, request, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }
}
