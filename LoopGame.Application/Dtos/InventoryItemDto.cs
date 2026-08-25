namespace LoopGame.Application.Dtos;

public record InventoryItemDto(
    int InventoryId,
    int ItemId,
    string ItemKey,
    string DisplayName,
    string Category,
    DateTime PurchasedAt,
    decimal EgpPaid);
