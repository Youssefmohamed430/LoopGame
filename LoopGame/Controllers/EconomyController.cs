using System.Security.Claims;
using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Controllers;

/// <summary>
/// Economy endpoints (UC-ECO-01..05). Balance mutations flow exclusively through
/// IEconomyService. Reset is intentionally NOT exposed here — the UC-GAME-11
/// orchestrator (game-progress group) owns the full reset flow.
/// </summary>
[Authorize]
[ApiController]
[Route("api/economy")]
public class EconomyController(IEconomyService _economy) : ControllerBase
{
    [HttpGet("{playerId:int}/balance")]
    public async Task<ActionResult<BalanceDto>> GetBalance(int playerId, CancellationToken ct)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        return await Handle(_economy.GetBalanceAsync(playerId, ct));
    }

    [HttpGet("{playerId:int}/transactions")]
    public async Task<ActionResult<PagedResult<TransactionDto>>> GetTransactions(
        int playerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        return await Handle(_economy.GetTransactionHistoryAsync(playerId, page, pageSize, ct));
    }

    /// <summary>Administrative gateway for manual balance adjustments and compensations.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{playerId:int}/delta")]
    public async Task<ActionResult<decimal>> ApplyDelta(
        int playerId, [FromBody] ApplyEgpDeltaRequest request, CancellationToken ct)
        => await Handle(_economy.ApplyEgpDeltaAsync(
            playerId, request.Delta, request.TransactionType, request.Description, request.ReferenceId, ct));

    [HttpPost("{playerId:int}/salary/{shiftId:int}")]
    public async Task<ActionResult<decimal>> PayShiftSalary(int playerId, int shiftId, CancellationToken ct)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        return await Handle(_economy.PayShiftSalaryAsync(playerId, shiftId, ct));
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

    private async Task<ActionResult> Handle<T>(Task<Result<T>> operation)
    {
        var result = await operation;
        if (result.IsFailure)
            return result.Error.ToActionResult();
        return Ok(result.Value);
    }

    public record ApplyEgpDeltaRequest(decimal Delta, TransactionType TransactionType, string Description, int? ReferenceId);
}
