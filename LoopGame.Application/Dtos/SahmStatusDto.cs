namespace LoopGame.Application.Dtos;

public record SahmStatusDto(
    string Tier,
    int DailyHintLimit,
    int HintsUsedToday,
    int HintsRemaining,
    DateTime ResetsAtUtc);
