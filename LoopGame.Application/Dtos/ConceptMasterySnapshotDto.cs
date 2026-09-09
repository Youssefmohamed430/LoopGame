namespace LoopGame.Application.Dtos;

/// <summary>
/// Read-only projection of <see cref="ConceptMasterySnapshot"/> for API consumers.
/// </summary>
public record ConceptMasterySnapshotDto(
    int SnapshotId,
    int PlayerId,
    int ShiftId,
    Concept ConceptTag,
    decimal MasteryScore,
    int EvidenceCount,
    DateTime SnapshottedAt);
