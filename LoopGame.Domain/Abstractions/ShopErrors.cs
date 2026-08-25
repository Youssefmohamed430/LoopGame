namespace LoopGame.Domain.Abstractions;

public static class ShopErrors
{
    public static readonly Error ItemNotFoundOrUnavailable = new("Shop.ItemNotFoundOrUnavailable", "The requested shop item does not exist or is not available.");
    public static readonly Error RankNotMet                = new("Shop.RankNotMet", "The player's rank does not meet the item requirement.");
    public static readonly Error AlreadyOwned              = new("Shop.AlreadyOwned", "The player already owns this item.");
}
