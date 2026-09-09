namespace LoopGame.Application.Dtos.SideTaskDtos;

/// <summary>Active side task as seen by the player.</summary>
public record SideTaskDto(
    int       SideTaskId,
    string    Title,
    string    Description,
    decimal   EgpReward,
    string    Status,           // Active / Submitted / Abandoned / Expired
    Concept   ConceptTag
);

/// <summary>Player submits their code solution for a side task.</summary>
public record SideTaskSubmitRequestDto(
    int    SideTaskId,
    string SubmittedCode,
    int    TimeSpentSec,
    byte   SahmHintsUsed
);

/// <summary>Result returned after a player abandons a side task.</summary>
public record AbandonResultDto(
    decimal PenaltyApplied,     // always -100 EGP
    decimal NewBalance
);

// ─── Hint DTOs ────────────────────────────────────────────────────────────────

/// <summary>
/// Player-facing view of a single pre-generated hint.
/// HintText is null when the hint is still locked (privacy guard).
/// </summary>
public record SideTaskHintDto(
    int      HintId,
    int      HintLevel,      // 1 = ConceptualNudge, 2 = StructuralGuidance, 3 = CodeSnippet
    string   HintLevelName,
    decimal  EgpCost,
    bool     IsUnlocked,
    string?  HintText        // null when locked
);

/// <summary>Player requests to unlock a specific hint level for their active task.</summary>
public record UnlockHintRequestDto(
    int SideTaskId,
    int HintLevel    // 1, 2, or 3
);

/// <summary>Result of a successful hint unlock, including updated balance.</summary>
public record UnlockHintResultDto(
    int     HintId,
    string  HintText,
    decimal EgpCost,
    decimal NewBalance
);
