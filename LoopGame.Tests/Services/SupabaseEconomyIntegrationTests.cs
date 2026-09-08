using Infrastructure;
using Infrastructure.Repositories;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Application.Services.EconomyAndProgressionServices;
using LoopGame.Domain.Abstractions;
using LoopGame.Domain.Entities.Economy;
using LoopGame.Domain.Entities.Player;
using LoopGame.Domain.Enums;
using LoopGame.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace LoopGame.Tests.Services;

[Trait("Category", "Integration")]
public class SupabaseEconomyIntegrationTests : IAsyncLifetime
{
    private readonly string _connectionString;
    private int _testPlayerId;
    private int _testItemId;

    public SupabaseEconomyIntegrationTests()
    {
        _connectionString = LoadConnectionString();
    }

    private static string LoadConnectionString()
    {
        var envPath = Path.Combine(AppContext.BaseDirectory, "../../../../.env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (line.StartsWith("ConnectionStrings__DefaultConnection="))
                {
                    return line["ConnectionStrings__DefaultConnection=".Length..].Trim().Trim('"');
                }
            }
        }
        return "Host=aws-1-eu-west-1.pooler.supabase.com;Database=postgres;Username=postgres.fijgkqlfgpkyfjwncqec;Password=LoopGame#0000;SSL Mode=Require;Trust Server Certificate=true";
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        using var db = CreateDbContext();

        // Find an existing player in Supabase
        var player = await db.Players.FirstOrDefaultAsync();
        if (player == null)
            throw new InvalidOperationException("No existing player found in Supabase to run integration tests against.");

        _testPlayerId = player.PlayerId;

        // Clean any existing test economy data for this player
        await CleanPlayerDataAsync(db, _testPlayerId);

        // Seed a test shop item
        var existingItem = await db.ShopItems.FirstOrDefaultAsync(i => i.ItemKey == "test_plant");
        if (existingItem == null)
        {
            existingItem = new ShopItem
            {
                ItemKey = "test_plant",
                DisplayName = "Test Desk Plant",
                Category = "desk_item",
                Price = 150m,
                IsAvailable = true
            };
            db.ShopItems.Add(existingItem);
            await db.SaveChangesAsync();
        }
        _testItemId = existingItem.ItemId;

        // Seed fresh PlayerEconomy for test player
        var economy = new PlayerEconomy(_testPlayerId);
        economy.Credit(1000m, TransactionType.Bonus, "Initial test seed");
        db.PlayerEconomies.Add(economy);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        using var db = CreateDbContext();
        await CleanPlayerDataAsync(db, _testPlayerId);

        var item = await db.ShopItems.FirstOrDefaultAsync(i => i.ItemKey == "test_plant");
        if (item != null)
        {
            db.ShopItems.Remove(item);
            await db.SaveChangesAsync();
        }
    }

    private static async Task CleanPlayerDataAsync(AppDbContext db, int playerId)
    {
        var inventories = await db.PlayerInventories.Where(i => i.PlayerId == playerId).ToListAsync();
        db.PlayerInventories.RemoveRange(inventories);

        var txs = await db.Transactions.Where(t => t.PlayerId == playerId).ToListAsync();
        db.Transactions.RemoveRange(txs);

        var sahm = await db.SahmSubscriptions.Where(s => s.PlayerId == playerId).ToListAsync();
        db.SahmSubscriptions.RemoveRange(sahm);

        var economies = await db.PlayerEconomies.Where(e => e.PlayerId == playerId).ToListAsync();
        db.PlayerEconomies.RemoveRange(economies);

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Supabase_CheckConstraint_RejectsNegativeBalance()
    {
        using var db = CreateDbContext();

        // Attempt direct SQL update violating CHK_Economy_Balance
        var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await db.Database.ExecuteSqlAsync(
                $"UPDATE \"PlayerEconomy\" SET \"Balance\" = -50 WHERE \"PlayerId\" = {_testPlayerId}");
        });

        Assert.Equal("23514", ex.SqlState); // check_violation
        Assert.Contains("CHK_Economy_Balance", ex.ConstraintName ?? ex.Message);
    }

    [Fact]
    public async Task Supabase_CheckConstraint_RejectsInvalidSalaryTier()
    {
        using var db = CreateDbContext();

        // Attempt direct SQL update violating CHK_Economy_SalaryTier (must be 1..5)
        var ex = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await db.Database.ExecuteSqlAsync(
                $"UPDATE \"PlayerEconomy\" SET \"SalaryTier\" = 99 WHERE \"PlayerId\" = {_testPlayerId}");
        });

        Assert.Equal("23514", ex.SqlState); // check_violation
        Assert.Contains("CHK_Economy_SalaryTier", ex.ConstraintName ?? ex.Message);
    }

    [Fact]
    public async Task Supabase_RowLock_GetForUpdate_ExecutesNatively()
    {
        using var db = CreateDbContext();
        var uow = new UnitOfWork(db);
        var repo = new PlayerEconomyRepository(db);

        await uow.BeginTransactionAsync();

        // SELECT ... FOR UPDATE natively against Supabase PostgreSQL
        var lockedEconomy = await repo.GetForUpdateAsync(_testPlayerId);

        Assert.NotNull(lockedEconomy);
        Assert.Equal(_testPlayerId, lockedEconomy.PlayerId);
        Assert.Equal(1000m, lockedEconomy.Balance);

        await uow.RollbackAsync();
    }

    [Fact]
    public async Task Supabase_Transaction_Rollback_LeavesBalanceAndLedgerUntouched()
    {
        using var db = CreateDbContext();
        var uow = new UnitOfWork(db);
        var repo = new PlayerEconomyRepository(db);
        var service = new EconomyService(uow, repo);

        // Balance before
        var before = await service.GetBalanceAsync(_testPlayerId);
        Assert.Equal(1000m, before.Value.Balance);

        // Begin transaction, apply delta, then force rollback
        await uow.BeginTransactionAsync();
        var credit = await service.ApplyEgpDeltaAsync(_testPlayerId, 500m, TransactionType.Bonus, "Rolled back credit");
        Assert.True(credit.IsSuccess);
        Assert.Equal(1500m, credit.Value);

        await uow.RollbackAsync();

        // Verify with fresh DbContext that nothing persisted
        using var freshDb = CreateDbContext();
        var freshUow = new UnitOfWork(freshDb);
        var freshRepo = new PlayerEconomyRepository(freshDb);
        var freshService = new EconomyService(freshUow, freshRepo);

        var after = await freshService.GetBalanceAsync(_testPlayerId);
        Assert.Equal(1000m, after.Value.Balance);

        var txCount = await freshDb.Transactions
            .CountAsync(t => t.PlayerId == _testPlayerId && t.Description == "Rolled back credit");
        Assert.Equal(0, txCount);
    }

    [Fact]
    public async Task Supabase_SalaryPerShift_UniqueFilteredIndex_EnforcedByDatabase()
    {
        using var db = CreateDbContext();

        // First salary transaction
        var tx1 = new Transaction
        {
            PlayerId = _testPlayerId,
            Amount = 500m,
            TransactionType = TransactionType.Salary,
            Description = "Shift 100 salary",
            ReferenceId = 100,
            BalanceAfter = 1500m
        };
        db.Transactions.Add(tx1);
        await db.SaveChangesAsync();

        // Second salary transaction for the SAME shift and player
        var tx2 = new Transaction
        {
            PlayerId = _testPlayerId,
            Amount = 500m,
            TransactionType = TransactionType.Salary,
            Description = "Duplicate Shift 100 salary",
            ReferenceId = 100,
            BalanceAfter = 2000m
        };
        db.Transactions.Add(tx2);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await db.SaveChangesAsync();
        });

        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23505", pgEx.SqlState); // unique_violation
        Assert.Contains("UX_Transaction_SalaryPerShift", pgEx.ConstraintName ?? pgEx.Message);
    }

    [Fact]
    public async Task Supabase_PlayerInventory_UniqueIndex_EnforcedByDatabase()
    {
        using var db = CreateDbContext();

        var inv1 = new PlayerInventory
        {
            PlayerId = _testPlayerId,
            ItemId = _testItemId,
            EgpPaid = 150m
        };
        db.PlayerInventories.Add(inv1);
        await db.SaveChangesAsync();

        var inv2 = new PlayerInventory
        {
            PlayerId = _testPlayerId,
            ItemId = _testItemId,
            EgpPaid = 150m
        };
        db.PlayerInventories.Add(inv2);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            await db.SaveChangesAsync();
        });

        var pgEx = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal("23505", pgEx.SqlState); // unique_violation
        Assert.Contains("UQ_PlayerInventory", pgEx.ConstraintName ?? pgEx.Message);
    }

    [Fact]
    public async Task Supabase_ShopService_PurchaseItem_ExecutesAtomically()
    {
        using var db = CreateDbContext();
        var uow = new UnitOfWork(db);
        var repo = new PlayerEconomyRepository(db);
        var shopService = new ShopService(uow, repo);

        // Balance starts at 1000m. Item price is 150m.
        var result = await shopService.PurchaseItemAsync(_testPlayerId, _testItemId);

        Assert.True(result.IsSuccess);
        Assert.Equal(850m, result.Value.NewBalance);
        Assert.Equal(150m, result.Value.PricePaid);

        // Verify with fresh DbContext directly against Supabase
        using var verifyDb = CreateDbContext();
        var economy = await verifyDb.PlayerEconomies.SingleAsync(e => e.PlayerId == _testPlayerId);
        Assert.Equal(850m, economy.Balance);
        Assert.Equal(150m, economy.TotalSpent);

        var inventory = await verifyDb.PlayerInventories
            .SingleOrDefaultAsync(i => i.PlayerId == _testPlayerId && i.ItemId == _testItemId);
        Assert.NotNull(inventory);
        Assert.Equal(150m, inventory.EgpPaid);

        var tx = await verifyDb.Transactions
            .SingleOrDefaultAsync(t => t.PlayerId == _testPlayerId && t.TransactionType == TransactionType.Purchase && t.ReferenceId == _testItemId);
        Assert.NotNull(tx);
        Assert.Equal(-150m, tx.Amount);
        Assert.Equal(850m, tx.BalanceAfter);

        // Attempt purchasing again: Guard 4 and UQ_PlayerInventory reject it
        var duplicateResult = await shopService.PurchaseItemAsync(_testPlayerId, _testItemId);
        Assert.True(duplicateResult.IsFailure);
        Assert.Equal(ShopErrors.AlreadyOwned, duplicateResult.Error);
    }
}
