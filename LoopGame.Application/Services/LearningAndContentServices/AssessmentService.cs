using LoopGame.Domain.Constants;
using Microsoft.Extensions.Logging;

namespace LoopGame.Application.Services.LearningAndContentServices;

/// <summary>
/// Core stealth-assessment business logic.
/// • Records raw immutable evidence (<see cref="AssessmentEvent"/>).
/// • Computes concept mastery using weighted scoring + recency decay + sigmoid.
/// • Never called directly from gameplay HTTP requests — invoked by Hangfire jobs.
/// </summary>
public class AssessmentService(
    IUnitOfWork _uow,
    ILogger<AssessmentService> _logger) : IAssessmentService
{
    // ══════════════════════════════════════════════════════════════════════
    // Event recording
    // ══════════════════════════════════════════════════════════════════════

    public async Task<Result> RecordEventAsync(AssessmentEventDto dto, CancellationToken ct = default)
    {
        if (!AssessmentWeights.EventTypes.All.Contains(dto.EventType))
            return Result.Failure(AssessmentErrors.InvalidEventType);

        var entity = dto.Adapt<AssessmentEvent>();

        await _uow.GetRepository<AssessmentEvent>().AddAsync(entity);
        await _uow.SaveAsync(ct);

        _logger.LogDebug(
            "Recorded assessment event {EventType} for player {PlayerId}, concept {ConceptTag}",
            dto.EventType, dto.PlayerId, dto.ConceptTag);

        return Result.Success();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Mastery computation
    // ══════════════════════════════════════════════════════════════════════

    public async Task<Result> ComputeMasteryAsync(int playerId, int shiftId, CancellationToken ct = default)
    {
        var player = await _uow.GetRepository<Player>()
            .FindAsync(p => p.PlayerId == playerId);

        if (player is null)
            return Result.Failure(AssessmentErrors.PlayerNotFound);

        var shift = await _uow.GetRepository<Shift>()
            .FindAsync(s => s.ShiftId == shiftId);

        if (shift is null)
            return Result.Failure(AssessmentErrors.ShiftNotFound);

        // Fetch all events for this player (across all shifts — events aren't shift-scoped,
        // but we snapshot per shift using PracticeTask.ShiftId / StoryBeat.ShiftId associations).
        var events = await _uow.GetRepository<AssessmentEvent>()
            .FindAll(e => e.PlayerId == playerId)
            .OrderBy(e => e.RecordedAt)
            .ToListAsync(ct);

        if (events.Count == 0)
            return Result.Failure(AssessmentErrors.NoEventsFound);

        // Filter out non-learning evidence events (e.g. GateCleared, ShiftCompleted telemetry) early
        var genuineEvidenceEvents = events
            .Where(e => !string.IsNullOrWhiteSpace(e.ConceptTag) && !IsProgressionEvent(e.EventType));

        // Group genuine learning evidence by concept tag
        var conceptGroups = genuineEvidenceEvents.GroupBy(e => e.ConceptTag!);

        var now = DateTime.UtcNow;
        var snapshotRepo = _uow.GetRepository<ConceptMasterySnapshot>();

        foreach (var group in conceptGroups)
        {
            var conceptTag = group.Key;
            double weightedSum = 0.0;
            double decayDenominator = 0.0;

            foreach (var evt in group)
            {
                double weight = GetEventWeight(evt);
                double decay = ComputeRecencyDecay(evt.RecordedAt, now);

                weightedSum += weight * decay;
                decayDenominator += decay;
            }

            // Sigmoid normalisation to [0, 1] strictly when genuine learning evidence exists
            decimal mastery = 0m;
            if (decayDenominator > 0)
            {
                double rawScore = weightedSum / decayDenominator;
                mastery = Math.Clamp((decimal)Sigmoid(rawScore), 0m, 1m);
            }

            // Upsert: find existing snapshot for (player, shift, concept)
            var existing = snapshotRepo.FindWithTracking(
                s => s.PlayerId == playerId &&
                     s.ShiftId == shiftId &&
                     s.ConceptTag == conceptTag);

            if (existing is not null)
            {
                existing.MasteryScore  = mastery;
                existing.EvidenceCount = group.Count();
                existing.SnapshottedAt = now;
            }
            else
            {
                await snapshotRepo.AddAsync(new ConceptMasterySnapshot
                {
                    PlayerId      = playerId,
                    ShiftId       = shiftId,
                    ConceptTag    = conceptTag,
                    MasteryScore  = mastery,
                    EvidenceCount = group.Count(),
                    SnapshottedAt = now
                });
            }
        }

        await _uow.SaveAsync(ct);

        _logger.LogInformation(
            "Computed mastery for player {PlayerId}, shift {ShiftId}: {ConceptCount} concepts",
            playerId, shiftId, conceptGroups.Count());

        return Result.Success();
    }

    // ══════════════════════════════════════════════════════════════════════
    // Mastery retrieval
    // ══════════════════════════════════════════════════════════════════════

    public async Task<Result<IEnumerable<ConceptMasterySnapshotDto>>> GetPlayerMasteryAsync(
        int playerId, CancellationToken ct = default)
    {
        var player = await _uow.GetRepository<Player>()
            .FindAsync(p => p.PlayerId == playerId);

        if (player is null)
            return Result.Failure<IEnumerable<ConceptMasterySnapshotDto>>(AssessmentErrors.PlayerNotFound);

        var snapshots = await _uow.GetRepository<ConceptMasterySnapshot>()
            .FindAll(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.SnapshottedAt)
            .Select(s => new ConceptMasterySnapshotDto(
                s.SnapshotId,
                s.PlayerId,
                s.ShiftId,
                s.ConceptTag,
                s.MasteryScore,
                s.EvidenceCount,
                s.SnapshottedAt))
            .ToListAsync(ct);

        return Result.Success<IEnumerable<ConceptMasterySnapshotDto>>(snapshots);
    }

    public async Task<Result<IEnumerable<ConceptMasterySnapshotDto>>> GetWeakestConceptsAsync(
        int playerId, int topN = 3, CancellationToken ct = default)
    {
        var player = await _uow.GetRepository<Player>()
            .FindAsync(p => p.PlayerId == playerId);

        if (player is null)
            return Result.Failure<IEnumerable<ConceptMasterySnapshotDto>>(AssessmentErrors.PlayerNotFound);

        // For each concept, take the latest snapshot and order by lowest mastery
        var snapshots = await _uow.GetRepository<ConceptMasterySnapshot>()
            .FindAll(s => s.PlayerId == playerId)
            .GroupBy(s => s.ConceptTag)
            .Select(g => g.OrderByDescending(s => s.SnapshottedAt).First())
            .OrderBy(s => s.MasteryScore)
            .Take(topN)
            .Select(s => new ConceptMasterySnapshotDto(
                s.SnapshotId,
                s.PlayerId,
                s.ShiftId,
                s.ConceptTag,
                s.MasteryScore,
                s.EvidenceCount,
                s.SnapshottedAt))
            .ToListAsync(ct);

        return Result.Success<IEnumerable<ConceptMasterySnapshotDto>>(snapshots);
    }

    // ══════════════════════════════════════════════════════════════════════
    // Private helpers — weighting, decay, normalisation, classification
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Progression and telemetry events track player progression but do NOT constitute concept mastery evidence.
    /// </summary>
    private static bool IsProgressionEvent(string eventType) =>
        eventType is AssessmentWeights.EventTypes.GateCleared
                  or AssessmentWeights.EventTypes.ShiftCompleted
                  or AssessmentWeights.EventTypes.ConsequenceTrigger
                  or AssessmentWeights.EventTypes.DesktopInteraction;

    /// <summary>
    /// Maps an assessment event to its weight based on event type and tier.
    /// </summary>
    private static double GetEventWeight(AssessmentEvent evt)
    {
        return evt.EventType switch
        {
            // Progression / telemetry events do not contribute independent mastery evidence
            AssessmentWeights.EventTypes.GateCleared          => 0.0,
            AssessmentWeights.EventTypes.ShiftCompleted       => 0.0,
            AssessmentWeights.EventTypes.ConsequenceTrigger    => 0.0,
            AssessmentWeights.EventTypes.DesktopInteraction    => 0.0,

            AssessmentWeights.EventTypes.PracticeAttempt => evt.Tier switch
            {
                nameof(ChoiceTier.Ideal)      => AssessmentWeights.PracticeIdeal,
                nameof(ChoiceTier.Acceptable)  => AssessmentWeights.PracticeAcceptable,
                nameof(ChoiceTier.Debt)        => AssessmentWeights.PracticeDebt,
                nameof(ChoiceTier.Mistake)     => AssessmentWeights.PracticeMistake,
                _                              => AssessmentWeights.PracticeMistake
            },

            AssessmentWeights.EventTypes.ChoiceSubmission => AssessmentWeights.ChoiceIdeal,

            AssessmentWeights.EventTypes.HintRequest => AssessmentWeights.HintRequest,

            AssessmentWeights.EventTypes.SideTaskSubmission => AssessmentWeights.SideTask,

            _ => 0.0
        };
    }

    /// <summary>
    /// Exponential recency decay: recent events contribute more.
    /// decay(t) = 2^(-Δdays / halfLife)
    /// </summary>
    private static double ComputeRecencyDecay(DateTime eventTime, DateTime now)
    {
        var ageDays = (now - eventTime).TotalDays;
        if (ageDays < 0) ageDays = 0;
        return Math.Pow(2, -ageDays / AssessmentWeights.DecayHalfLifeDays);
    }

    /// <summary>
    /// Sigmoid normalisation: σ(x) = 1 / (1 + e^(-k*(x - midpoint)))
    /// Maps arbitrary weighted scores to [0, 1].
    /// </summary>
    private static double Sigmoid(double x)
    {
        return 1.0 / (1.0 + Math.Exp(-AssessmentWeights.SigmoidK * (x - AssessmentWeights.SigmoidMidpoint)));
    }
}