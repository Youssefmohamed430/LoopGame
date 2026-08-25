using LoopGame.Application.IServices.EconomyAndProgressionServices;

namespace LoopGame.Application.Services.EconomyAndProgressionServices;

/// <summary>
/// Sole application-layer writer of PlayerEconomy.Balance (HARD RULE 1).
/// Every balance change produces exactly one immutable Transaction ledger row
/// with BalanceAfter, persisted in the SAME database transaction (HARD RULE 2).
/// Money transactions lock the economy row FIRST and stay short — no HTTP/AI/timer
/// calls inside (HARD RULE 5). AssessmentEvent telemetry is never written here.
/// </summary>
public class EconomyService(
    IUnitOfWork _uow,
    IPlayerEconomyRepository _economyRepo) : IEconomyService
{
    private const int MaxPageSize = 100;

    public async Task<Result<BalanceDto>> GetBalanceAsync(int playerId, CancellationToken ct = default)
    {
        var economy = await _uow.GetRepository<PlayerEconomy>()
            .FindAll(e => e.PlayerId == playerId)
            .Select(e => new { e.Balance, e.TotalEarned, e.TotalSpent, e.SalaryTier })
            .FirstOrDefaultAsync(ct);

        if (economy is null)
            return Result.Failure<BalanceDto>(EconomyErrors.PlayerEconomyNotFound);

        return new BalanceDto(
            economy.Balance,
            economy.TotalEarned,
            economy.TotalSpent,
            SalaryTierName(economy.SalaryTier));
    }

    public async Task<Result<PagedResult<TransactionDto>>> GetTransactionHistoryAsync(
        int playerId, int page, int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaxPageSize)
            return Result.Failure<PagedResult<TransactionDto>>(EconomyErrors.InvalidPagination);

        // OrderByDescending(CreatedAt) rides IX_Transaction_Player_Date;
        // Take(pageSize + 1) detects HasNext; projection executes server-side.
        var rows = await _uow.GetRepository<Transaction>()
            .FindAll(t => t.PlayerId == playerId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(t => new TransactionDto(
                t.TransactionId,
                t.Amount,
                t.TransactionType,
                t.Description,
                t.BalanceAfter,
                t.CreatedAt))
            .ToListAsync(ct);

        var hasNext = rows.Count > pageSize;
        if (hasNext)
            rows.RemoveAt(rows.Count - 1);

        return new PagedResult<TransactionDto>(rows, page, pageSize, hasNext);
    }

    public async Task<Result<decimal>> ApplyEgpDeltaAsync(
        int playerId, decimal delta, TransactionType type, string description,
        int? referenceId = null, CancellationToken ct = default)
    {
        if (delta == 0m)
            return Result.Failure<decimal>(EconomyErrors.InvalidAmount);

        await _uow.BeginTransactionAsync(ct);
        try
        {
            // Lock the economy row FIRST.
            var economy = await _economyRepo.GetForUpdateAsync(playerId, ct);
            if (economy is null)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure<decimal>(EconomyErrors.PlayerEconomyNotFound);
            }

            Transaction ledger;
            if (delta > 0m)
            {
                ledger = economy.Credit(delta, type, description, referenceId);
            }
            else if (type == TransactionType.Penalty)
            {
                ledger = economy.ApplyPenalty(-delta, description, referenceId);
            }
            else
            {
                var debit = economy.TryDebit(-delta, type, description, referenceId);
                if (debit.IsFailure)
                {
                    await _uow.RollbackAsync(ct);
                    return Result.Failure<decimal>(debit.Error);
                }
                ledger = debit.Value;
            }

            await _uow.GetRepository<Transaction>().AddAsync(ledger);
            await _uow.SaveAsync(ct);
            await _uow.CommitAsync(ct);

            return economy.Balance;
        }
        catch
        {
            // Cleanup must not be cancellable: a cancelled ct must not mask the
            // original exception or leave the transaction open.
            await _uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<Result<decimal>> PayShiftSalaryAsync(int playerId, int shiftId, CancellationToken ct = default)
    {
        // Lock-first transaction: idempotency check, rank and tier reads ALL happen
        // inside the transaction after the economy row is locked (review F-1),
        // serializing concurrent salary calls for the same player.
        // The filtered unique index UX_Transaction_SalaryPerShift is the DB backstop.
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var economy = await _economyRepo.GetForUpdateAsync(playerId, ct);
            if (economy is null)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure<decimal>(EconomyErrors.PlayerEconomyNotFound);
            }

            var txRepo = _uow.GetRepository<Transaction>();

            // (a) Idempotency guard: salary for this shift already paid?
            var alreadyPaid = await txRepo
                .FindAll(t => t.PlayerId == playerId
                           && t.TransactionType == TransactionType.Salary
                           && t.ReferenceId == shiftId)
                .AnyAsync(ct);

            if (alreadyPaid)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure<decimal>(EconomyErrors.SalaryAlreadyPaid);
            }

            // (b) Player rank (no-tracking projection).
            var rank = await _uow.GetRepository<Player>()
                .FindAll(p => p.PlayerId == playerId)
                .Select(p => (PlayerRank?)p.Rank)
                .FirstOrDefaultAsync(ct);

            if (rank is null)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure<decimal>(EconomyErrors.PlayerNotFound);
            }

            // (c) Tier distribution of the shift's choices, aggregated server-side.
            var tierCounts = await _uow.GetRepository<PlayerChoice>()
                .FindAll(pc => pc.PlayerId == playerId && pc.Beat.ShiftId == shiftId)
                .GroupBy(pc => pc.Tier)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            // (d) Total = base salary + performance bonus.
            var baseSalary = SalaryPolicy.BaseSalary(rank.Value);
            var total = baseSalary + SalaryPolicy.ComputeShiftBonus(baseSalary, tierCounts);

            // (e) Credit + ledger + single save + commit. DB-only work: stays short.
            var ledger = economy.Credit(total, TransactionType.Salary, $"Shift {shiftId} salary", shiftId);

            await txRepo.AddAsync(ledger);
            await _uow.SaveAsync(ct);
            await _uow.CommitAsync(ct);

            return economy.Balance;
        }
        catch
        {
            // Cleanup must not be cancellable: a cancelled ct must not mask the
            // original exception or leave the transaction open.
            await _uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<Result> ResetEconomyAsync(int playerId, CancellationToken ct = default)
    {
        // ONE transaction: lock economy → Reset → wipe inventory & ledger rows.
        await _uow.BeginTransactionAsync(ct);
        try
        {
            var economy = await _economyRepo.GetForUpdateAsync(playerId, ct);
            if (economy is null)
            {
                await _uow.RollbackAsync(ct);
                return Result.Failure(EconomyErrors.PlayerEconomyNotFound);
            }

            economy.Reset();

            var inventoryRepo = _uow.GetRepository<PlayerInventory>();
            var inventoryRows = await inventoryRepo
                .FindAll(i => i.PlayerId == playerId)
                .ToListAsync(ct);
            foreach (var row in inventoryRows)
                inventoryRepo.Delete(row);

            var txRepo = _uow.GetRepository<Transaction>();
            var ledgerRows = await txRepo
                .FindAll(t => t.PlayerId == playerId)
                .ToListAsync(ct);
            foreach (var row in ledgerRows)
                txRepo.Delete(row);

            await _uow.SaveAsync(ct);
            await _uow.CommitAsync(ct);

            return Result.Success();
        }
        catch
        {
            // Cleanup must not be cancellable: a cancelled ct must not mask the
            // original exception or leave the transaction open.
            await _uow.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    // SalaryTier is stored 1–5 (1 = Intern) while the PlayerRank enum is 0-based.
    private static string SalaryTierName(int salaryTier)
        => salaryTier is >= 1 and <= 5
            ? ((PlayerRank)(salaryTier - 1)).ToString()
            : $"Unknown({salaryTier})";
}
