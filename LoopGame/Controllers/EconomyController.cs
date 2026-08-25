using LoopGame.Application.Dtos;
using LoopGame.Application.Services;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Controllers;

/// <summary>
/// Economy endpoints (UC-ECO-01..05). Balance mutations flow exclusively through
/// IEconomyService. Reset is intentionally NOT exposed here — the UC-GAME-11
/// orchestrator (game-progress group) owns the full reset flow.
/// TODO(identity): replace {playerId} route param with authenticated principal
/// once the auth pipeline lands.
/// </summary>
[ApiController]
[Route("api/economy")]
public class EconomyController(IEconomyService _economy) : ControllerBase
{
    [HttpGet("{playerId:int}/balance")]
    public async Task<ActionResult<BalanceDto>> GetBalance(int playerId, CancellationToken ct)
        => await Handle(_economy.GetBalanceAsync(playerId, ct));

    [HttpGet("{playerId:int}/transactions")]
    public async Task<ActionResult<PagedResult<TransactionDto>>> GetTransactions(
        int playerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => await Handle(_economy.GetTransactionHistoryAsync(playerId, page, pageSize, ct));

    /// <summary>Gateway for OTHER groups' money effects — the only sanctioned writer path besides salary.</summary>
    [HttpPost("{playerId:int}/delta")]
    public async Task<ActionResult<decimal>> ApplyDelta(
        int playerId, [FromBody] ApplyEgpDeltaRequest request, CancellationToken ct)
        => await Handle(_economy.ApplyEgpDeltaAsync(
            playerId, request.Delta, request.TransactionType, request.Description, request.ReferenceId, ct));

    [HttpPost("{playerId:int}/salary/{shiftId:int}")]
    public async Task<ActionResult<decimal>> PayShiftSalary(int playerId, int shiftId, CancellationToken ct)
        => await Handle(_economy.PayShiftSalaryAsync(playerId, shiftId, ct));

    private async Task<ActionResult> Handle<T>(Task<Result<T>> operation)
    {
        var result = await operation;
        if (result.IsFailure)
            return result.Error.ToActionResult();
        return Ok(result.Value);
    }

    public record ApplyEgpDeltaRequest(decimal Delta, TransactionType TransactionType, string Description, int? ReferenceId);
}
