using System.Security.Claims;
using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Controllers;

/// <summary>
/// Virtual shop endpoints: catalogue browsing, purchases (incl. Sahm tier
/// upgrades) and owned inventory (UC-ECO-06/08/09).
/// </summary>
[Authorize]
[ApiController]
[Route("api/shop")]
public class ShopController(IShopService _shop) : ControllerBase
{
    [HttpGet("{playerId:int}/catalog")]
    public async Task<ActionResult<IReadOnlyList<ShopItemDto>>> GetCatalog(int playerId, CancellationToken ct)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        var result = await _shop.GetCatalogAsync(playerId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    [HttpGet("{playerId:int}/inventory")]
    public async Task<ActionResult<IReadOnlyList<InventoryItemDto>>> GetInventory(int playerId, CancellationToken ct)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        var result = await _shop.GetInventoryAsync(playerId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    [HttpPost("{playerId:int}/purchase/{itemId:int}")]
    public async Task<ActionResult<PurchaseResultDto>> Purchase(int playerId, int itemId, CancellationToken ct)
    {
        if (!IsAuthorizedForPlayer(playerId))
            return Forbid();

        var result = await _shop.PurchaseItemAsync(playerId, itemId, ct);
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
