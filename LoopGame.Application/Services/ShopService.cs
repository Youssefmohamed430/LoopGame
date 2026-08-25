namespace LoopGame.Application.Services;

/// <summary>
/// Shop catalogue browsing and purchases (UC-ECO-06, UC-ECO-09).
/// Balance mutations go EXCLUSIVELY through the PlayerEconomy domain methods
/// (TryDebit) inside a lock-first transaction — same discipline as EconomyService.
/// Guard order per SHIFT_Backend_Architecture.md §5.9:
/// item available → rank met → balance sufficient → not already owned
/// (sahm_tier one-way validation runs with the other guards, before any mutation).
/// </summary>
public class ShopService(
    IUnitOfWork _uow,
    IPlayerEconomyRepository _economyRepo) : IShopService
{
    public async Task<Result<IReadOnlyList<ShopItemDto>>> GetCatalogAsync(int playerId, CancellationToken ct = default)
    {
        // Server-side projection; IsOwned translates to an EXISTS subquery.
        // RankRequired is projected raw (enum ToString does not translate to SQL)
        // and mapped in memory after materialization.
        var rows = await _uow.GetRepository<ShopItem>()
            .FindAll(i => i.IsAvailable)
            .OrderBy(i => i.SortOrder)
            .Select(i => new
            {
                i.ItemId,
                i.ItemKey,
                i.DisplayName,
                i.Category,
                i.Description,
                i.Price,
                i.RankRequired,
                i.SortOrder,
                IsOwned = i.PlayerInventories.Any(pi => pi.PlayerId == playerId)
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new ShopItemDto(
                r.ItemId,
                r.ItemKey,
                r.DisplayName,
                r.Category,
                r.Description,
                r.Price,
                r.RankRequired.HasValue ? r.RankRequired.Value.ToString() : null,
                r.IsOwned,
                r.SortOrder))
            .ToList();

        return Result.Success<IReadOnlyList<ShopItemDto>>(items);
    }

    public async Task<Result<PurchaseResultDto>> PurchaseItemAsync(int playerId, int itemId, CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(ct);
        try
        {
            // Lock the economy row FIRST.
            var economy = await _economyRepo.GetForUpdateAsync(playerId, ct);
            if (economy is null)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure<PurchaseResultDto>(EconomyErrors.PlayerEconomyNotFound);
            }

            // Guard 1: item must exist and be available.
            var item = await _uow.GetRepository<ShopItem>()
                .FindAll(i => i.ItemId == itemId && i.IsAvailable)
                .Select(i => new { i.ItemKey, i.DisplayName, i.Category, i.Price, i.RankRequired })
                .FirstOrDefaultAsync(ct);

            if (item is null)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure<PurchaseResultDto>(ShopErrors.ItemNotFoundOrUnavailable);
            }

            // Guard 2: player rank must meet the requirement.
            var rank = await _uow.GetRepository<Player>()
                .FindAll(p => p.PlayerId == playerId)
                .Select(p => (PlayerRank?)p.Rank)
                .FirstOrDefaultAsync(ct);

            if (rank is null)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure<PurchaseResultDto>(EconomyErrors.PlayerNotFound);
            }

            if (item.RankRequired.HasValue && (int)rank.Value < (int)item.RankRequired.Value)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure<PurchaseResultDto>(ShopErrors.RankNotMet);
            }

            // Guard 3: balance sufficient (read-only pre-check keeps the documented
            // error ordering — the authoritative debit happens via TryDebit below).
            if (economy.Balance < item.Price)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure<PurchaseResultDto>(EconomyErrors.InsufficientBalance);
            }

            // Sahm tier upgrades: validate one-way ordering BEFORE any mutation.
            SahmTier? newSahmTier = null;
            if (item.Category == "sahm_tier")
            {
                if (!SahmTierPolicy.TryParseFromItemKey(item.ItemKey, out var targetTier))
                {
                    await _uow.RollbackAsync(ct);
                    return Result.Failure<PurchaseResultDto>(SahmErrors.InvalidTierUpgrade);
                }

                var currentTier = await GetActiveSahmTierAsync(playerId, ct);
                if ((int)targetTier <= (int)currentTier)
                {
                    await _uow.RollbackAsync(ct);
                    return Result.Failure<PurchaseResultDto>(SahmErrors.InvalidTierUpgrade);
                }

                newSahmTier = targetTier;
            }

            // Guard 4: not already owned (UNIQUE constraint is the DB backstop).
            var owned = await _uow.GetRepository<PlayerInventory>()
                .FindAll(pi => pi.PlayerId == playerId && pi.ItemId == itemId)
                .AnyAsync(ct);

            if (owned)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure<PurchaseResultDto>(ShopErrors.AlreadyOwned);
            }

            // Mutation: single debit producing the immutable ledger row.
            var debit = economy.TryDebit(item.Price, TransactionType.Purchase, $"Purchased {item.DisplayName}", itemId);
            if (debit.IsFailure)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure<PurchaseResultDto>(debit.Error);
            }
            await _uow.GetRepository<Transaction>().AddAsync(debit.Value);

            var inventoryRow = new PlayerInventory
            {
                PlayerId = playerId,
                ItemId   = itemId,
                EgpPaid  = item.Price
            };
            await _uow.GetRepository<PlayerInventory>().AddAsync(inventoryRow);

            if (newSahmTier.HasValue)
            {
                await _uow.GetRepository<SahmSubscription>().AddAsync(new SahmSubscription
                {
                    PlayerId       = playerId,
                    Tier           = newSahmTier.Value,
                    DailyHintLimit = SahmTierPolicy.GetDailyHintLimit(newSahmTier.Value),
                    HintsUsedToday = 0
                });
            }

            await _uow.SaveAsync(ct);
            await _uow.CommitAsync(ct);

            return new PurchaseResultDto(itemId, item.ItemKey, item.Price, economy.Balance, newSahmTier);
        }
        catch
        {
            // Cleanup must not be cancellable: a cancelled ct must not mask the
            // original exception or leave the transaction open.
            await _uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<Result<IReadOnlyList<InventoryItemDto>>> GetInventoryAsync(int playerId, CancellationToken ct = default)
    {
        var rows = await _uow.GetRepository<PlayerInventory>()
            .FindAll(pi => pi.PlayerId == playerId)
            .OrderByDescending(pi => pi.PurchasedAt)
            .Select(pi => new InventoryItemDto(
                pi.InventoryId,
                pi.ItemId,
                pi.Item.ItemKey,
                pi.Item.DisplayName,
                pi.Item.Category,
                pi.PurchasedAt,
                pi.EgpPaid))
            .ToListAsync(ct);

        return Result.Success<IReadOnlyList<InventoryItemDto>>(rows);
    }

    /// <summary>Active tier = latest row by ActivatedAt (SahmSubscription is a history model). Defaults to Free.</summary>
    private async Task<SahmTier> GetActiveSahmTierAsync(int playerId, CancellationToken ct)
        => await _uow.GetRepository<SahmSubscription>()
            .FindAll(s => s.PlayerId == playerId)
            .OrderByDescending(s => s.ActivatedAt)
            .Select(s => (SahmTier?)s.Tier)
            .FirstOrDefaultAsync(ct) ?? SahmTier.Free;
}
