namespace LoopGame.Domain.Entities.Economy;

/// <summary>
/// Catalogue of items available in the LoopOS shop.
/// Category: sahm_tier | camera | desk_item | workspace.
/// </summary>
public class ShopItem
{
    public int         ItemId      { get; set; }
    public string      ItemKey     { get; set; } = string.Empty;
    public string      DisplayName { get; set; } = string.Empty;

    /// <summary>sahm_tier | camera | desk_item | workspace</summary>
    public string      Category    { get; set; } = string.Empty;

    public string?     Description { get; set; }
    public decimal     Price       { get; set; } // CHECK > 0
    public PlayerRank? RankRequired { get; set; }
    public bool        IsOneWay    { get; set; } = false;
    public string?     AssetKey    { get; set; }
    public bool        IsAvailable { get; set; } = true;
    public int         SortOrder   { get; set; } = 0;

    // Navigation
    public ICollection<PlayerInventory> PlayerInventories { get; set; } = [];
}
