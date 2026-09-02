using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Domain.Constants;

namespace LoopGame.Application.Services.EconomyAndProgressionServices;

/// <summary>
/// Sahm AI assistant subscription & daily hint management (UC-SAHM-02..07).
/// Subscription is a HISTORY model — active tier = latest row by ActivatedAt.
/// Daily counters reset lazily on first request of a new UTC day (SD-SAHM-01)
/// plus an explicit bulk reset for the midnight scheduler (SD-SAHM-03).
/// Telemetry is emitted fire-and-forget AFTER persistence — never inside any
/// money transaction (rule 6).
/// </summary>
public class SahmService(
    IUnitOfWork _uow,
    IEventPublisher _eventPublisher) : ISahmService
{
    public async Task<Result<HintResponseDto>> RequestHintAsync(
        int playerId, HintRequestDto request, CancellationToken ct = default)
    {
        // TRACKED read: we mutate the counter below.
        var subscription = await GetLatestSubscriptionTrackedAsync(playerId, ct);

        if (subscription is null)
        {
            // Lazy-create the implicit Free subscription on first use.
            subscription = new SahmSubscription
            {
                PlayerId       = playerId,
                Tier           = SahmTier.Free,
                DailyHintLimit = SahmTierPolicy.GetDailyHintLimit(SahmTier.Free),
                HintsUsedToday = 0,
                LastHintReset  = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            await _uow.GetRepository<SahmSubscription>().AddAsync(subscription);
        }
        else if (subscription.LastHintReset < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            // Lazy reset (double-safety with the midnight job, SD-SAHM-01 note 4).
            subscription.HintsUsedToday = 0;
            subscription.LastHintReset  = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var limit = subscription.DailyHintLimit;
        if (subscription.HintsUsedToday >= limit)
            return Result.Failure<HintResponseDto>(SahmErrors.DailyHintLimitReached);

        subscription.HintsUsedToday++;

        await _uow.SaveAsync(ct); // single save for this use case

        // Telemetry: fire-and-forget, after persistence, outside any money transaction.
        _eventPublisher.Publish(new GameEventDto(
            playerId,
            EventType: AssessmentWeights.EventTypes.HintRequest,
            ConceptTag: request.ConceptTag,
            Tier: subscription.Tier.ToString(),
            PayloadJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                concept  = request.ConceptTag,
                hintLevel = (int)MapHintLevel(subscription.Tier)
            })));

        var resetsAtUtc = DateTime.UtcNow.Date.AddDays(1); // next midnight UTC

        return new HintResponseDto(
            subscription.Tier.ToString(),
            MapHintLevel(subscription.Tier),
            subscription.HintsUsedToday,
            limit - subscription.HintsUsedToday,
            resetsAtUtc,
            HintText: null);
    }

    public async Task<Result<SahmStatusDto>> GetStatusAsync(int playerId, CancellationToken ct = default)
    {
        var subscription = await GetLatestSubscriptionTrackedAsync(playerId, ct);

        var tier = subscription?.Tier ?? SahmTier.Free;
        var limit = subscription?.DailyHintLimit ?? SahmTierPolicy.GetDailyHintLimit(SahmTier.Free);
        var used = subscription?.HintsUsedToday ?? 0;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Report through a lazy-reset lens without persisting anything (read-only use case).
        if (subscription != null && subscription.LastHintReset < today)
            used = 0;

        return new SahmStatusDto(
            tier.ToString(),
            limit,
            used,
            Math.Max(0, limit - used),
            DateTime.UtcNow.Date.AddDays(1));
    }

    public async Task<Result<int>> ResetExpiredCountersAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Resolve keys via a no-tracking projection, then mutate TRACKED loads
        // (FindAll is AsNoTracking in this codebase's BaseRepository).
        var repo = _uow.GetRepository<SahmSubscription>();
        var staleIds = await repo
            .FindAll(s => s.LastHintReset < today)
            .Select(s => s.SubscriptionId)
            .ToListAsync(ct);

        foreach (var id in staleIds)
        {
            var subscription = repo.FindWithTracking(s => s.SubscriptionId == id);
            if (subscription is null) continue;

            subscription.HintsUsedToday = 0;
            subscription.LastHintReset  = today;
        }

        if (staleIds.Count > 0)
            await _uow.SaveAsync(ct);

        return staleIds.Count;
    }

    private async Task<SahmSubscription?> GetLatestSubscriptionTrackedAsync(int playerId, CancellationToken ct)
    {
        // Resolve the latest row's key via a no-tracking projection, then load it
        // TRACKED (FindWithTracking) — the counter is mutated by the caller.
        var latestId = await _uow.GetRepository<SahmSubscription>()
            .FindAll(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.ActivatedAt)
            .Select(s => (int?)s.SubscriptionId)
            .FirstOrDefaultAsync(ct);

        if (latestId is null)
            return null;

        return _uow.GetRepository<SahmSubscription>().FindWithTracking(s => s.SubscriptionId == latestId.Value);
    }

    /// <summary>Free → conceptual nudges only; Pro → structural guidance + snippets; Team/Enterprise → full detail.</summary>
    private static HintLevel MapHintLevel(SahmTier tier) => tier switch
    {
        SahmTier.Pro        => HintLevel.StructuralGuidance,
        SahmTier.Team       => HintLevel.CodeSnippet,
        SahmTier.Enterprise => HintLevel.CodeSnippet,
        _                   => HintLevel.ConceptualNudge
    };
}
