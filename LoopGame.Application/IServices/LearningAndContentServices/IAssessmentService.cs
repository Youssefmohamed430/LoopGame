namespace LoopGame.Application.IServices.LearningAndContentServices;

/// <summary>
/// Stealth assessment business logic: records raw evidence, computes
/// concept mastery from accumulated assessment events, and provides
/// mastery data for dashboards and the future AI orchestration layer.
/// </summary>
public interface IAssessmentService
{
    /// <summary>
    /// Persists a single assessment event as immutable evidence.
    /// Called by background jobs only — never from a gameplay HTTP request.
    /// </summary>
    Task<Result> RecordEventAsync(AssessmentEventDto assessmentEvent, CancellationToken ct = default);

    /// <summary>
    /// Aggregates all assessment events for a player within a shift, applies
    /// weighted scoring + recency decay + sigmoid normalisation, and upserts
    /// <see cref="ConceptMasterySnapshot"/> rows.
    /// </summary>
    Task<Result> ComputeMasteryAsync(int playerId, int shiftId, CancellationToken ct = default);

    /// <summary>Returns the latest mastery snapshots for a player across all shifts.</summary>
    Task<Result<IEnumerable<ConceptMasterySnapshotDto>>> GetPlayerMasteryAsync(int playerId, CancellationToken ct = default);

    /// <summary>
    /// Returns the player's weakest concepts (lowest mastery scores)
    /// for use by dashboards and the future AI layer.
    /// </summary>
    Task<Result<IEnumerable<ConceptMasterySnapshotDto>>> GetWeakestConceptsAsync(int playerId, int topN = 3, CancellationToken ct = default);
}