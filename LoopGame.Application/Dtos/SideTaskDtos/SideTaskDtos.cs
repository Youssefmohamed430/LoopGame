namespace LoopGame.Application.Dtos.SideTaskDtos;

/// <summary>Active side task as seen by the player.</summary>
public record SideTaskDto(
    int       SideTaskId,
    string    Title,
    string    Description,
    decimal   EgpReward,
    DateTime? DeadlineAt,
    string    Status,           // Active / Submitted / Abandoned / Expired
    string    ConceptTag
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
