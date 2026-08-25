namespace LoopGame.Application.Dtos;

public record HintResponseDto(
    string Tier,
    HintLevel HintLevel,
    int HintsUsedToday,
    int HintsRemaining,
    DateTime ResetsAtUtc,

    /// <summary>null until the AI-pipeline group's IAiOrchestrationService is wired in.</summary>
    string? HintText);
