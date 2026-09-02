using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Application.Services.EconomyAndProgressionServices;
using Microsoft.EntityFrameworkCore;

namespace LoopGame.Tests.Services;

/// <summary>
/// Unit tests for SahmService (UC-SAHM-02/03/04/06/07) over the EF InMemory
/// harness. Covers lazy daily reset, limit enforcement, counter increments,
/// hint-level mapping per tier, status reporting and the midnight bulk reset.
/// The emitter is a spy — verifying telemetry is fire-and-forget AFTER save.
/// </summary>
public class SahmServiceTests : IDisposable
{
    private const int PlayerId = 1;

    private readonly AppDbContext _db;
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly List<GameEventDto> _emitted = [];
    private readonly SahmService _sut;

    public SahmServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _uow.Setup(u => u.GetRepository<SahmSubscription>())
            .Returns(new BaseRepository<SahmSubscription>(_db));

        _uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => _db.SaveChangesAsync(ct));

        var publisher = new Mock<IEventPublisher>();
        publisher.Setup(e => e.Publish(It.IsAny<GameEventDto>()))
               .Callback<GameEventDto>(e => _emitted.Add(e));

        _sut = new SahmService(_uow.Object, publisher.Object);
    }

    private async Task<SahmSubscription> SeedSubscriptionAsync(
        SahmTier tier, byte limit, int usedToday, DateOnly? lastReset = null)
    {
        var sub = new SahmSubscription
        {
            PlayerId       = PlayerId,
            Tier           = tier,
            DailyHintLimit = limit,
            HintsUsedToday = (byte)usedToday,
            LastHintReset  = lastReset ?? DateOnly.FromDateTime(DateTime.UtcNow)
        };
        _db.SahmSubscriptions.Add(sub);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        return sub;
    }

    private static HintRequestDto Request(string conceptTag = "loops") =>
        new(TaskId: 5, TaskType: "practice", ConceptTag: conceptTag);

    // ── RequestHintAsync ─────────────────────────────────────────────

    [Fact]
    public async Task RequestHint_FirstEverUse_LazilyCreatesFreeSubscription_AndConsumesOne()
    {
        var result = await _sut.RequestHintAsync(PlayerId, Request());

        Assert.True(result.IsSuccess);
        Assert.Equal("Free", result.Value.Tier);
        Assert.Equal(HintLevel.ConceptualNudge, result.Value.HintLevel);
        Assert.Equal(1, result.Value.HintsUsedToday);
        Assert.Equal(2, result.Value.HintsRemaining); // Free limit = 3

        var sub = await _db.SahmSubscriptions.SingleAsync(s => s.PlayerId == PlayerId);
        Assert.Equal(SahmTier.Free, sub.Tier);
        Assert.Equal(1, sub.HintsUsedToday);

        Assert.Single(_emitted);
        Assert.Equal("hint_request", _emitted[0].EventType);
        Assert.Equal("Free", _emitted[0].Tier);
    }

    [Fact]
    public async Task RequestHint_UnderLimit_IncrementsCounter_PersistsAndEmits()
    {
        await SeedSubscriptionAsync(SahmTier.Pro, limit: 10, usedToday: 4);

        var result = await _sut.RequestHintAsync(PlayerId, Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(HintLevel.StructuralGuidance, result.Value.HintLevel);
        Assert.Equal(5, result.Value.HintsUsedToday);
        Assert.Equal(5, result.Value.HintsRemaining);

        // Counter persisted to the DB (tracked update), not just tracked state
        Assert.Equal(5, (await _db.SahmSubscriptions.SingleAsync(s => s.PlayerId == PlayerId)).HintsUsedToday);
        Assert.Single(_emitted);
    }

    [Fact]
    public async Task RequestHint_LimitReached_Fails_DoesNotEmit()
    {
        await SeedSubscriptionAsync(SahmTier.Free, limit: 3, usedToday: 3);

        var result = await _sut.RequestHintAsync(PlayerId, Request());

        Assert.True(result.IsFailure);
        Assert.Equal(SahmErrors.DailyHintLimitReached, result.Error);
        Assert.Empty(_emitted); // rejected request → no telemetry

        Assert.Equal(3, (await _db.SahmSubscriptions.SingleAsync(s => s.PlayerId == PlayerId)).HintsUsedToday);
    }

    [Fact]
    public async Task RequestHint_NewDay_LazyResetsCounter_BeforeChecking()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        await SeedSubscriptionAsync(SahmTier.Team, limit: 25, usedToday: 25, lastReset: yesterday);

        var result = await _sut.RequestHintAsync(PlayerId, Request());

        // Yesterday's exhausted quota must NOT block today's first hint
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.HintsUsedToday);
        Assert.Equal(24, result.Value.HintsRemaining);

        var sub = await _db.SahmSubscriptions.SingleAsync(s => s.PlayerId == PlayerId);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), sub.LastHintReset);
    }

    [Theory]
    [InlineData(SahmTier.Free, HintLevel.ConceptualNudge)]
    [InlineData(SahmTier.Pro, HintLevel.StructuralGuidance)]
    [InlineData(SahmTier.Team, HintLevel.CodeSnippet)]
    [InlineData(SahmTier.Enterprise, HintLevel.CodeSnippet)]
    public async Task RequestHint_HintLevel_MapsFromTier(SahmTier tier, HintLevel expectedLevel)
    {
        byte limit = tier switch
        {
            SahmTier.Free => (byte)3,
            SahmTier.Pro => (byte)10,
            SahmTier.Team => (byte)25,
            _ => byte.MaxValue
        };
        await SeedSubscriptionAsync(tier, limit, usedToday: 0);

        var result = await _sut.RequestHintAsync(PlayerId, Request());

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedLevel, result.Value.HintLevel);
        Assert.Null(result.Value.HintText); // AI text out of scope until pipeline lands
    }

    // ── GetStatusAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetStatus_NoSubscription_ReturnsFreeDefaults_WithoutPersisting()
    {
        var result = await _sut.GetStatusAsync(PlayerId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Free", result.Value.Tier);
        Assert.Equal(3, result.Value.DailyHintLimit);
        Assert.Equal(0, result.Value.HintsUsedToday);
        Assert.Equal(3, result.Value.HintsRemaining);
        Assert.True(result.Value.ResetsAtUtc > DateTime.UtcNow);

        Assert.Empty(await _db.SahmSubscriptions.ToListAsync()); // read-only: nothing created
    }

    [Fact]
    public async Task GetStatus_StaleCounter_ReportsThroughLazyResetLens()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        await SeedSubscriptionAsync(SahmTier.Pro, limit: 10, usedToday: 9, lastReset: yesterday);

        var result = await _sut.GetStatusAsync(PlayerId);

        Assert.True(result.IsSuccess);
        Assert.Equal("Pro", result.Value.Tier);
        Assert.Equal(0, result.Value.HintsUsedToday);   // new day
        Assert.Equal(10, result.Value.HintsRemaining);
    }

    // ── ResetExpiredCountersAsync (midnight job) ────────────────────

    [Fact]
    public async Task ResetExpiredCounters_ResetsOnlyStaleRows_ReturnsCount()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var staleA = new SahmSubscription { PlayerId = 10, Tier = SahmTier.Free, DailyHintLimit = 3, HintsUsedToday = 3, LastHintReset = yesterday };
        var staleB = new SahmSubscription { PlayerId = 11, Tier = SahmTier.Pro, DailyHintLimit = 10, HintsUsedToday = 7, LastHintReset = yesterday };
        var fresh  = new SahmSubscription { PlayerId = 12, Tier = SahmTier.Team, DailyHintLimit = 25, HintsUsedToday = 20, LastHintReset = today };
        _db.SahmSubscriptions.AddRange(staleA, staleB, fresh);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.ResetExpiredCountersAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);

        Assert.Equal(0, (await _db.SahmSubscriptions.SingleAsync(s => s.PlayerId == 10)).HintsUsedToday);
        Assert.Equal(20, (await _db.SahmSubscriptions.SingleAsync(s => s.PlayerId == 12)).HintsUsedToday); // untouched
    }

    public void Dispose() => _db.Dispose();
}
