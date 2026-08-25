namespace LoopGame.Application.IServices.EconomyAndProgressionServices;

public interface IShopService
{
    /// <summary>Browses the available catalogue with an IsOwned badge per player (UC-ECO browse).</summary>
    Task<Result<IReadOnlyList<ShopItemDto>>> GetCatalogAsync(int playerId, CancellationToken ct = default);

    /// <summary>Purchases a shop item: guards → debit via economy domain model → inventory row (+ SahmSubscription for sahm_tier items) in ONE transaction (UC-ECO-06 / UC-ECO-09).</summary>
    Task<Result<PurchaseResultDto>> PurchaseItemAsync(int playerId, int itemId, CancellationToken ct = default);

    /// <summary>Lists the player's owned items, newest first (view owned inventory).</summary>
    Task<Result<IReadOnlyList<InventoryItemDto>>> GetInventoryAsync(int playerId, CancellationToken ct = default);
}
