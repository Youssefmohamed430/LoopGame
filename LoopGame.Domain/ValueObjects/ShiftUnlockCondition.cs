namespace LoopGame.Domain.ValueObjects;

/// <summary>
/// Deserialized representation of the Shift.unlock_condition JSON column.
/// Describes prerequisites that must be met before a player can start the shift.
/// </summary>
public record ShiftUnlockCondition(
    [property: JsonPropertyName("prerequisite_shift_id")] int? PrerequisiteShiftId,
    [property: JsonPropertyName("min_rank")]              string? MinRank,
    [property: JsonPropertyName("required_concept")]      string? RequiredConcept,
    [property: JsonPropertyName("min_mastery_score")]     decimal? MinMasteryScore
);
