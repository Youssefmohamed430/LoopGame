namespace LoopGame.Domain.Entities.Economy;

/// <summary>
/// Records items owned by a player.
/// Composite unique key: (PlayerId, ItemId).
/// </summary>
public class PlayerInventory
{
    public int      InventoryId  { get; set; }
    public int      PlayerId     { get; set; }
    public int      ItemId       { get; set; }
    public DateTime PurchasedAt  { get; set; } = DateTime.UtcNow;
    public decimal  EgpPaid      { get; set; }

    // Navigation
    public Player.Player Player { get; set; } = null!;
    public ShopItem       Item   { get; set; } = null!;
}
