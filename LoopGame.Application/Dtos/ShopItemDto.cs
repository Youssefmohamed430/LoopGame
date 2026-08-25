namespace LoopGame.Application.Dtos;

public record ShopItemDto(
    int ItemId,
    string ItemKey,
    string DisplayName,
    string Category,
    string? Description,
    decimal Price,
    string? RankRequired,
    bool IsOwned,
    int SortOrder);
