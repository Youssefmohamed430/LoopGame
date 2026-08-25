namespace LoopGame.Application.Dtos;

public record PurchaseResultDto(
    int ItemId,
    string ItemKey,
    decimal PricePaid,
    decimal NewBalance,
    SahmTier? NewSahmTier);
