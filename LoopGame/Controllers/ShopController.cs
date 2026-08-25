using LoopGame.Application.Dtos;
using LoopGame.Application.Services;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Controllers;

/// <summary>
/// Virtual shop endpoints: catalogue browsing, purchases (incl. Sahm tier
/// upgrades) and owned inventory (UC-ECO-06/08/09).
/// TODO(identity): replace {playerId} route param with authenticated principal.
/// </summary>
[ApiController]
[Route("api/shop")]
public class ShopController(IShopService _shop) : ControllerBase
{
    [HttpGet("{playerId:int}/catalog")]
    public async Task<ActionResult<IReadOnlyList<ShopItemDto>>> GetCatalog(int playerId, CancellationToken ct)
    {
        var result = await _shop.GetCatalogAsync(playerId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    [HttpGet("{playerId:int}/inventory")]
    public async Task<ActionResult<IReadOnlyList<InventoryItemDto>>> GetInventory(int playerId, CancellationToken ct)
    {
        var result = await _shop.GetInventoryAsync(playerId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }

    [HttpPost("{playerId:int}/purchase/{itemId:int}")]
    public async Task<ActionResult<PurchaseResultDto>> Purchase(int playerId, int itemId, CancellationToken ct)
    {
        var result = await _shop.PurchaseItemAsync(playerId, itemId, ct);
        return result.IsFailure ? result.Error.ToActionResult() : Ok(result.Value);
    }
}
