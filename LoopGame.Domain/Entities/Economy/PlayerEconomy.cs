namespace LoopGame.Domain.Entities.Economy;

/// <summary>
/// Single source of truth for a player's EGP balance and salary tier.
/// Composite unique key on player_id (1:1 with Player).
/// Balance/totals are only mutated through the domain methods below;
/// EconomyService is the sole application-layer writer.
/// </summary>
public class PlayerEconomy
{
    public int      EconomyId    { get; set; }
    public int      PlayerId     { get; set; }
    public decimal  Balance      { get; private set; } = 0m; // CHECK >= 0
    public int      SalaryTier   { get; set; } = 1;  // 1–5
    public decimal  TotalEarned  { get; private set; } = 0m;
    public decimal  TotalSpent   { get; private set; } = 0m;
    public DateTime UpdatedAt    { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Player.Player Player { get; set; } = null!;

    /// <summary>
    /// Credits the balance (amount must be > 0) and returns the ledger row
    /// to be persisted in the SAME database transaction.
    /// </summary>
    public Transaction Credit(decimal amount, TransactionType type, string description, int? referenceId = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Credit amount must be positive.");

        Balance += amount;
        TotalEarned += amount;
        Touch();
        return BuildLedger(amount, type, description, referenceId);
    }

    /// <summary>
    /// Attempts a debit. Fails with <see cref="EconomyErrors.InsufficientBalance"/>
    /// when amount <= 0 or Balance < amount. Never makes the balance negative.
    /// </summary>
    public Result<Transaction> TryDebit(decimal amount, TransactionType type, string description, int? referenceId = null)
    {
        if (amount <= 0 || Balance < amount)
            return Result.Failure<Transaction>(EconomyErrors.InsufficientBalance);

        Balance -= amount;
        TotalSpent += amount;
        Touch();
        return Result.Success(BuildLedger(-amount, type, description, referenceId));
    }

    /// <summary>
    /// Applies a penalty debited from the balance, clamped at zero:
    /// debits MIN(Balance, amount) so the balance never goes negative.
    /// </summary>
    public Transaction ApplyPenalty(decimal amount, string description, int? referenceId = null)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Penalty amount must be positive.");

        var applied = Math.Min(Balance, amount);
        Balance -= applied;
        TotalSpent += applied;
        Touch();
        return BuildLedger(-applied, TransactionType.Penalty, description, referenceId);
    }

    /// <summary>
    /// Full economy reset used by the new-game flow (UC-GAME-11).
    /// Ledger/inventory rows are deleted by the service in the same DB transaction.
    /// </summary>
    public void Reset()
    {
        Balance = 0m;
        TotalEarned = 0m;
        TotalSpent = 0m;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;

    private Transaction BuildLedger(decimal signedAmount, TransactionType type, string description, int? referenceId) => new()
    {
        PlayerId     = PlayerId,
        Amount       = signedAmount,
        TransactionType = type,
        Description  = description,
        ReferenceId  = referenceId,
        BalanceAfter = Balance
    };
}
