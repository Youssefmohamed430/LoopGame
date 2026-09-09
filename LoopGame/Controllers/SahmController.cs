using System.Security.Claims;
using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Controllers;

/// <summary>
/// Sahm AI assistant endpoints: hint requests (limit-enforced, lazy daily
/// reset) and subscription status (UC-SAHM-02/03/04/07).
/// </summary>
[Authorize]
[ApiController]
[Route("api/sahm")]
public class SahmController(ISahmService _sahm) : ControllerBase
{
    [HttpGet("{playerId:int}/status")]
    public async Task<ActionResult<SahmStatusDto>> GetStatus(int playerId, CancellationToken ct)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        var result = await _sahm.GetStatusAsync(playerId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    [HttpPost("{playerId:int}/hint")]
    public async Task<ActionResult<HintResponseDto>> RequestHint(
        int playerId, [FromBody] HintRequestDto request, CancellationToken ct)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        var result = await _sahm.RequestHintAsync(playerId, request, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    private bool IsAuthorizedForPlayer(int playerId)
    {
        if (User?.Identity?.IsAuthenticated != true)
            return false;

        if (User.IsInRole("Admin"))
            return true;

        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(sub, out var authId) && authId == playerId;
    }
}
