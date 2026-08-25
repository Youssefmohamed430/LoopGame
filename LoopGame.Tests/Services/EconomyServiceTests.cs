using Microsoft.EntityFrameworkCore;
using Infrastructure.Repositories;

namespace LoopGame.Tests.Services;

/// <summary>
/// Unit tests for EconomyService using an EF InMemory database behind the REAL
/// BaseRepository (so async LINQ, projections and GroupBy execute for real) and a
/// fake IPlayerEconomyRepository (FOR UPDATE raw SQL is provider-specific).
/// Transaction begin/commit/rollback are mocked no-ops; persistence semantics of
/// the explicit transaction are covered by the manual race test (see
/// Docs/Economy_RaceTest_Manual.md).
/// </summary>
public class EconomyServiceTests : IDisposable
{
    private const int PlayerId = 1;

    private readonly AppDbContext _db;
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly EconomyService _sut;

    public EconomyServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _uow.Setup(u => u.GetRepository<Transaction>())
            .Returns(new BaseRepository<Transaction>(_db));
        _uow.Setup(u => u.GetRepository<PlayerEconomy>())
            .Returns(new BaseRepository<PlayerEconomy>(_db));
        _uow.Setup(u => u.GetRepository<Player>())
            .Returns(new BaseRepository<Player>(_db));
        _uow.Setup(u => u.GetRepository<PlayerChoice>())
            .Returns(new BaseRepository<PlayerChoice>(_db));
        _uow.Setup(u => u.GetRepository<PlayerInventory>())
            .Returns(new BaseRepository<PlayerInventory>(_db));

        _uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => _db.SaveChangesAsync(ct));
        _uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Fake economy repository: InMemory can't run FOR UPDATE raw SQL.
        var economyRepo = new FakeEconomyRepository(_db);

        _sut = new EconomyService(_uow.Object, economyRepo);
    }

    private sealed class FakeEconomyRepository(AppDbContext db) : IPlayerEconomyRepository
    {
        public Task<PlayerEconomy?> GetForUpdateAsync(int playerId, CancellationToken ct = default)
            => db.PlayerEconomies.FirstOrDefaultAsync(p => p.PlayerId == playerId);
    }

    private async Task SeedEconomyAsync(decimal balance = 1000m)
    {
        var economy = new PlayerEconomy { PlayerId = PlayerId };
        _db.PlayerEconomies.Add(economy);
        await _db.SaveChangesAsync();
        if (balance > 0)
            _db.Transactions.Add(economy.Credit(balance, TransactionType.Bonus, "seed"));
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private async Task SeedPlayerAsync(PlayerRank rank = PlayerRank.Intern)
    {
        _db.Players.Add(new Player { PlayerId = PlayerId, Rank = rank });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task GetBalance_ReturnsRankName_AndTotals()
    {
        await SeedEconomyAsync(500m);
        var economy = await _db.PlayerEconomies.SingleAsync(e => e.PlayerId == PlayerId);
        economy.SalaryTier = 3; // ExperiencedJunior
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.GetBalanceAsync(PlayerId);

        Assert.True(result.IsSuccess);
        Assert.Equal(500m, result.Value.Balance);
        Assert.Equal(500m, result.Value.TotalEarned);
        Assert.Equal("ExperiencedJunior", result.Value.SalaryTier);
    }

    [Fact]
    public async Task GetBalance_MissingEconomy_Fails()
    {
        var result = await _sut.GetBalanceAsync(999);

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.PlayerEconomyNotFound, result.Error);
    }

    [Fact]
    public async Task ApplyEgpDelta_Positive_CreditsBalance_AndInsertsLedgerRow()
    {
        await SeedEconomyAsync();

        var result = await _sut.ApplyEgpDeltaAsync(PlayerId, 250m, TransactionType.BugBounty, "Bounty", referenceId: 9);

        Assert.True(result.IsSuccess);
        Assert.Equal(1250m, result.Value);
        Assert.Equal(1250m, result.Value); // balance snapshot

        var ledger = await _db.Transactions.SingleAsync(t => t.PlayerId == PlayerId && t.ReferenceId == 9);
        Assert.Equal(250m, ledger.Amount);
        Assert.Equal(TransactionType.BugBounty, ledger.TransactionType);
        Assert.Equal(1250m, ledger.BalanceAfter);
    }

    [Fact]
    public async Task ApplyEgpDelta_Zero_FailsWithInvalidAmount_WithoutTouchingState()
    {
        await SeedEconomyAsync();

        var result = await _sut.ApplyEgpDeltaAsync(PlayerId, 0m, TransactionType.Purchase, "noop");

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.InvalidAmount, result.Error);
        Assert.Empty(_db.Transactions.Local);
    }

    [Fact]
    public async Task PayShiftSalary_MissingEconomyRow_Fails()
    {
        await SeedPlayerAsync(PlayerRank.Intern); // player exists, economy row does not

        var result = await _sut.PayShiftSalaryAsync(PlayerId, shiftId: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.PlayerEconomyNotFound, result.Error);
        Assert.Empty(await _db.Transactions.ToListAsync()); // nothing persisted
    }

    [Fact]
    public async Task ApplyEgpDelta_MissingEconomy_FailsAndRollsBack()
    {
        var result = await _sut.ApplyEgpDeltaAsync(999, 50m, TransactionType.Purchase, "x");

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.PlayerEconomyNotFound, result.Error);
        _uow.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApplyEgpDelta_NegativePenalty_ClampsAtZero()
    {
        await SeedEconomyAsync(30m);

        var result = await _sut.ApplyEgpDeltaAsync(PlayerId, -100m, TransactionType.Penalty, "Abandonment");

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value);
        var ledger = await _db.Transactions.SingleAsync(t => t.TransactionType == TransactionType.Penalty);
        Assert.Equal(-30m, ledger.Amount); // only what was applied
    }

    [Fact]
    public async Task ApplyEgpDelta_NegativePurchase_WithInsufficientBalance_Fails()
    {
        await SeedEconomyAsync(50m);

        var result = await _sut.ApplyEgpDeltaAsync(PlayerId, -80m, TransactionType.Purchase, "Camera");

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.InsufficientBalance, result.Error);
        // Rolled back: only the pre-existing seed row remains, nothing new persisted.
        var remaining = await _db.Transactions.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(TransactionType.Bonus, remaining[0].TransactionType);
    }

    [Fact]
    public async Task PayShiftSalary_AlreadyPaid_FailsWithoutPayingTwice()
    {
        await SeedEconomyAsync();
        _db.Transactions.Add(new Transaction
        {
            PlayerId = PlayerId,
            Amount = 2000m,
            TransactionType = TransactionType.Salary,
            Description = "Shift 1 salary",
            ReferenceId = 1,
            BalanceAfter = 3000m
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.PayShiftSalaryAsync(PlayerId, shiftId: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.SalaryAlreadyPaid, result.Error);
        Assert.Equal(1, await _db.Transactions.CountAsync(t => t.TransactionType == TransactionType.Salary));
    }

    [Fact]
    public async Task PayShiftSalary_ComputesBasePlusBonus_AndWritesLedgerOnce()
    {
        await SeedEconomyAsync(0m);
        await SeedPlayerAsync(PlayerRank.Intern); // base 2000

        var shift1 = new Shift { ShiftNumber = 1, ChapterNumber = 1, Title = "First Day" };
        _db.Shifts.Add(shift1);
        await _db.SaveChangesAsync();

        var beat = new StoryBeat { BeatKey = "b1", ShiftId = shift1.ShiftId, ContentJson = new StoryBeatContent("text", null, null, null) };
        _db.StoryBeats.Add(beat);
        await _db.SaveChangesAsync();

        // 10 choices: 5 Ideal (50%), 3 Acceptable (30%) → bonus rate 0.13 → bonus 260
        for (int i = 0; i < 5; i++)
            _db.PlayerChoices.Add(new PlayerChoice { PlayerId = PlayerId, BeatId = beat.BeatId, ChoiceId = i + 1, Tier = ChoiceTier.Ideal });
        for (int i = 0; i < 3; i++)
            _db.PlayerChoices.Add(new PlayerChoice { PlayerId = PlayerId, BeatId = beat.BeatId, ChoiceId = i + 10, Tier = ChoiceTier.Acceptable });
        _db.PlayerChoices.Add(new PlayerChoice { PlayerId = PlayerId, BeatId = beat.BeatId, ChoiceId = 20, Tier = ChoiceTier.Debt });
        _db.PlayerChoices.Add(new PlayerChoice { PlayerId = PlayerId, BeatId = beat.BeatId, ChoiceId = 21, Tier = ChoiceTier.Mistake });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.PayShiftSalaryAsync(PlayerId, shiftId: shift1.ShiftId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2260m, result.Value); // 2000 + 260

        var ledger = await _db.Transactions.SingleAsync(t => t.TransactionType == TransactionType.Salary);
        Assert.Equal(2260m, ledger.Amount);
        Assert.Equal($"Shift {shift1.ShiftId} salary", ledger.Description);
        Assert.Equal(shift1.ShiftId, ledger.ReferenceId);
        Assert.Equal(2260m, ledger.BalanceAfter);
    }

    [Fact]
    public async Task ResetEconomy_ZeroesEconomy_AndDeletesInventoryAndLedger()
    {
        await SeedEconomyAsync();
        var economy = await _db.PlayerEconomies.SingleAsync(e => e.PlayerId == PlayerId);
        _db.Transactions.Add(economy.Credit(400m, TransactionType.SideTask, "task"));
        await _db.SaveChangesAsync();

        _db.PlayerInventories.Add(new PlayerInventory { PlayerId = PlayerId, ItemId = 5, EgpPaid = 100m });
        _db.PlayerInventories.Add(new PlayerInventory { PlayerId = PlayerId, ItemId = 6, EgpPaid = 200m });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.ResetEconomyAsync(PlayerId);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, (await _db.PlayerEconomies.SingleAsync(e => e.PlayerId == PlayerId)).Balance);
        Assert.Empty(await _db.PlayerInventories.Where(i => i.PlayerId == PlayerId).ToListAsync());
        Assert.Empty(await _db.Transactions.Where(t => t.PlayerId == PlayerId).ToListAsync());
    }

    [Fact]
    public async Task GetTransactionHistory_Paginates_AndDetectsHasNext()
    {
        await SeedEconomyAsync();
        var economy = await _db.PlayerEconomies.SingleAsync(e => e.PlayerId == PlayerId);
        for (int i = 1; i <= 3; i++)
            _db.Transactions.Add(economy.Credit(10m * i, TransactionType.SideTask, $"task {i}"));
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var page1 = await _sut.GetTransactionHistoryAsync(PlayerId, page: 1, pageSize: 2);
        var page2 = await _sut.GetTransactionHistoryAsync(PlayerId, page: 2, pageSize: 2);

        Assert.True(page1.IsSuccess);
        Assert.Equal(2, page1.Value.Items.Count);
        Assert.True(page1.Value.HasNext);

        Assert.True(page2.IsSuccess);
        Assert.Equal(2, page2.Value.Items.Count); // 4 rows total: seed + 3 tasks
        Assert.False(page2.Value.HasNext);

        // Newest first ordering
        Assert.Equal(30m, page1.Value.Items[0].Amount);
        Assert.Equal(20m, page1.Value.Items[1].Amount);
        Assert.Equal(10m, page2.Value.Items[0].Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetTransactionHistory_InvalidPage_Fails(int page)
    {
        var result = await _sut.GetTransactionHistoryAsync(PlayerId, page);

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.InvalidPagination, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetTransactionHistory_InvalidPageSize_Fails(int pageSize)
    {
        var result = await _sut.GetTransactionHistoryAsync(PlayerId, page: 1, pageSize: pageSize);

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.InvalidPagination, result.Error);
    }

    public void Dispose() => _db.Dispose();
}
