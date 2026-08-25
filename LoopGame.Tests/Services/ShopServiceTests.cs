using LoopGame.Application.Services.EconomyAndProgressionServices;
using Microsoft.EntityFrameworkCore;

namespace LoopGame.Tests.Services;

/// <summary>
/// Unit tests for ShopService over the EF InMemory harness (same pattern as
/// EconomyServiceTests): real BaseRepository instances, Moq'd IUnitOfWork,
/// fake economy repository. Covers UC-ECO-06 guards, the sahm_tier upgrade
/// path (UC-ECO-09) and catalogue/inventory projections.
/// </summary>
public class ShopServiceTests : IDisposable
{
    private const int PlayerId = 1;

    private readonly AppDbContext _db;
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly ShopService _sut;

    public ShopServiceTests()
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
        _uow.Setup(u => u.GetRepository<ShopItem>())
            .Returns(new BaseRepository<ShopItem>(_db));
        _uow.Setup(u => u.GetRepository<PlayerInventory>())
            .Returns(new BaseRepository<PlayerInventory>(_db));
        _uow.Setup(u => u.GetRepository<SahmSubscription>())
            .Returns(new BaseRepository<SahmSubscription>(_db));

        _uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => _db.SaveChangesAsync(ct));
        _uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _sut = new ShopService(_uow.Object, new FakeEconomyRepository(_db));

        SeedShopItems();
    }

    private sealed class FakeEconomyRepository(AppDbContext db) : IPlayerEconomyRepository
    {
        public Task<PlayerEconomy?> GetForUpdateAsync(int playerId, CancellationToken ct = default)
            => db.PlayerEconomies.FirstOrDefaultAsync(p => p.PlayerId == playerId);
    }

    private void SeedShopItems()
    {
        _db.ShopItems.AddRange(
            new ShopItem { ItemId = 10, ItemKey = "camera_dslr", DisplayName = "DSLR Camera", Category = "camera", Price = 300m },
            new ShopItem { ItemId = 11, ItemKey = "desk_plant", DisplayName = "Desk Plant", Category = "desk_item", Price = 50m },
            new ShopItem { ItemId = 12, ItemKey = "sahm_pro", DisplayName = "Sahm Pro", Category = "sahm_tier", Price = 1500m },
            new ShopItem { ItemId = 13, ItemKey = "sahm_team", DisplayName = "Sahm Team", Category = "sahm_tier", Price = 4000m, RankRequired = PlayerRank.Senior },
            new ShopItem { ItemId = 14, ItemKey = "legacy_item", DisplayName = "Unavailable Item", Category = "workspace", Price = 100m, IsAvailable = false });
        _db.SaveChanges();
    }

    private async Task SeedEconomyAsync(decimal balance)
    {
        var economy = new PlayerEconomy { PlayerId = PlayerId };
        _db.PlayerEconomies.Add(economy);
        await _db.SaveChangesAsync();
        if (balance > 0)
            _db.Transactions.Add(economy.Credit(balance, TransactionType.Bonus, "seed"));
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    private async Task SeedPlayerAsync(PlayerRank rank)
    {
        _db.Players.Add(new Player { PlayerId = PlayerId, Rank = rank });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    // ── GetCatalogAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetCatalog_ReturnsAvailableOnly_WithOwnedBadges()
    {
        await SeedEconomyAsync(0m);
        _db.PlayerInventories.Add(new PlayerInventory { PlayerId = PlayerId, ItemId = 11, EgpPaid = 50m });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.GetCatalogAsync(PlayerId);

        Assert.True(result.IsSuccess);
        var items = result.Value;

        Assert.DoesNotContain(items, i => i.ItemId == 14); // unavailable → hidden
        Assert.Equal(4, items.Count);

        var owned = items.Single(i => i.ItemId == 11);
        Assert.True(owned.IsOwned);
        Assert.False(items.Single(i => i.ItemId == 10).IsOwned);

        // Rank badge is a readable name; locked items still listed
        var locked = items.Single(i => i.ItemId == 13);
        Assert.Equal("Senior", locked.RankRequired);
    }

    // ── PurchaseItemAsync — happy paths ─────────────────────────────

    [Fact]
    public async Task Purchase_HappyPath_DebitsBalance_InsertsInventoryAndLedger()
    {
        await SeedEconomyAsync(500m);
        await SeedPlayerAsync(PlayerRank.Fresh);

        var result = await _sut.PurchaseItemAsync(PlayerId, itemId: 10); // camera, 300

        Assert.True(result.IsSuccess);
        Assert.Equal(200m, result.Value.NewBalance);
        Assert.Null(result.Value.NewSahmTier);

        Assert.Single(await _db.PlayerInventories.ToListAsync());
        var inv = await _db.PlayerInventories.SingleAsync();
        Assert.Equal(10, inv.ItemId);
        Assert.Equal(300m, inv.EgpPaid);

        var ledger = await _db.Transactions.SingleAsync(t => t.TransactionType == TransactionType.Purchase);
        Assert.Equal(-300m, ledger.Amount);
        Assert.Equal(10, ledger.ReferenceId);
        Assert.Equal(200m, ledger.BalanceAfter);
    }

    [Fact]
    public async Task Purchase_SahmTierItem_CreatesSubscription_WithHintLimit_AndDebitsOnce()
    {
        await SeedEconomyAsync(2000m);
        await SeedPlayerAsync(PlayerRank.Intern);

        var result = await _sut.PurchaseItemAsync(PlayerId, itemId: 12); // sahm_pro, 1500

        Assert.True(result.IsSuccess);
        Assert.Equal(SahmTier.Pro, result.Value.NewSahmTier);
        Assert.Equal(500m, result.Value.NewBalance);

        var sub = await _db.SahmSubscriptions.SingleAsync(s => s.PlayerId == PlayerId);
        Assert.Equal(SahmTier.Pro, sub.Tier);
        Assert.Equal((byte)10, sub.DailyHintLimit);
        Assert.Equal((byte)0, sub.HintsUsedToday);
    }

    // ── PurchaseItemAsync — guard failures (no state change) ────────

    [Fact]
    public async Task Purchase_ItemMissingOrUnavailable_Fails()
    {
        await SeedEconomyAsync(1000m);
        await SeedPlayerAsync(PlayerRank.Lead);

        var missing = await _sut.PurchaseItemAsync(PlayerId, itemId: 999);
        var unavailable = await _sut.PurchaseItemAsync(PlayerId, itemId: 14);

        Assert.True(missing.IsFailure);
        Assert.Equal(ShopErrors.ItemNotFoundOrUnavailable, missing.Error);
        Assert.True(unavailable.IsFailure);
        Assert.Equal(ShopErrors.ItemNotFoundOrUnavailable, unavailable.Error);
    }

    [Fact]
    public async Task Purchase_RankNotMet_Fails()
    {
        await SeedEconomyAsync(10000m);
        await SeedPlayerAsync(PlayerRank.Intern); // needs Senior for item 13

        var result = await _sut.PurchaseItemAsync(PlayerId, itemId: 13);

        Assert.True(result.IsFailure);
        Assert.Equal(ShopErrors.RankNotMet, result.Error);
        Assert.Empty(await _db.PlayerInventories.ToListAsync());
    }

    [Fact]
    public async Task Purchase_InsufficientBalance_Fails_NoInventoryNoLedger()
    {
        await SeedEconomyAsync(100m);
        await SeedPlayerAsync(PlayerRank.Fresh);

        var result = await _sut.PurchaseItemAsync(PlayerId, itemId: 10); // costs 300

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.InsufficientBalance, result.Error);
        Assert.Empty(await _db.PlayerInventories.ToListAsync());

        // Only the seed row remains — purchase debit rolled back
        var ledgers = await _db.Transactions.ToListAsync();
        Assert.Single(ledgers);
        Assert.Equal(TransactionType.Bonus, ledgers[0].TransactionType);
    }

    [Fact]
    public async Task Purchase_AlreadyOwned_Fails()
    {
        await SeedEconomyAsync(1000m);
        await SeedPlayerAsync(PlayerRank.Fresh);
        _db.PlayerInventories.Add(new PlayerInventory { PlayerId = PlayerId, ItemId = 11, EgpPaid = 50m });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.PurchaseItemAsync(PlayerId, itemId: 11);

        Assert.True(result.IsFailure);
        Assert.Equal(ShopErrors.AlreadyOwned, result.Error);
        Assert.Equal(1, await _db.PlayerInventories.CountAsync(pi => pi.ItemId == 11));
    }

    [Fact]
    public async Task Purchase_MissingEconomyRow_Fails()
    {
        await SeedPlayerAsync(PlayerRank.Fresh);

        var result = await _sut.PurchaseItemAsync(PlayerId, itemId: 10);

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.PlayerEconomyNotFound, result.Error);
    }

    // ── Sahm one-way upgrade rules ───────────────────────────────────

    [Fact]
    public async Task Purchase_SahmTier_SameOrDowngrade_Fails_WithoutCharging()
    {
        await SeedEconomyAsync(5000m);
        await SeedPlayerAsync(PlayerRank.Senior); // meets rank for sahm_team so guards reach tier validation
        _db.SahmSubscriptions.Add(new SahmSubscription
        {
            PlayerId = PlayerId,
            Tier = SahmTier.Team,
            DailyHintLimit = 25
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var downgrade = await _sut.PurchaseItemAsync(PlayerId, itemId: 12); // Pro < Team
        var sameTier = await _sut.PurchaseItemAsync(PlayerId, itemId: 13);  // Team == Team

        Assert.True(downgrade.IsFailure);
        Assert.Equal(SahmErrors.InvalidTierUpgrade, downgrade.Error);
        Assert.True(sameTier.IsFailure);
        Assert.Equal(SahmErrors.InvalidTierUpgrade, sameTier.Error);

        // No money moved, no subscription written
        Assert.Equal(5000m, (await _db.PlayerEconomies.SingleAsync(e => e.PlayerId == PlayerId)).Balance);
        Assert.Empty(await _db.SahmSubscriptions.Where(s => s.Tier != SahmTier.Team).ToListAsync());
    }

    [Fact]
    public async Task Purchase_SahmTier_LatestSubscriptionWins_ActiveTier()
    {
        await SeedEconomyAsync(20000m);
        await SeedPlayerAsync(PlayerRank.Senior);
        _db.SahmSubscriptions.AddRange(
            new SahmSubscription { PlayerId = PlayerId, Tier = SahmTier.Free, DailyHintLimit = 3 },
            new SahmSubscription { PlayerId = PlayerId, Tier = SahmTier.Pro, DailyHintLimit = 10 });
        await _db.SaveChangesAsync();

        // Make the Pro row clearly the latest
        var pro = await _db.SahmSubscriptions.OrderBy(s => s.SubscriptionId).LastAsync();
        pro.ActivatedAt = DateTime.UtcNow.AddDays(1);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.PurchaseItemAsync(PlayerId, itemId: 13); // Team > active Pro

        Assert.True(result.IsSuccess);
        Assert.Equal(SahmTier.Team, result.Value.NewSahmTier);
    }

    // ── GetInventoryAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetInventory_ReturnsJoinedItemDetails_NewestFirst()
    {
        await SeedEconomyAsync(0m);
        _db.PlayerInventories.AddRange(
            new PlayerInventory { PlayerId = PlayerId, ItemId = 10, EgpPaid = 300m, PurchasedAt = DateTime.UtcNow.AddDays(-2) },
            new PlayerInventory { PlayerId = PlayerId, ItemId = 11, EgpPaid = 50m, PurchasedAt = DateTime.UtcNow.AddDays(-1) });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.GetInventoryAsync(PlayerId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(11, result.Value[0].ItemId);                       // newest first
        Assert.Equal("desk_plant", result.Value[0].ItemKey);            // joined from ShopItem
        Assert.Equal("Desk Plant", result.Value[0].DisplayName);
        Assert.Equal(300m, result.Value[1].EgpPaid);
    }

    public void Dispose() => _db.Dispose();
}
