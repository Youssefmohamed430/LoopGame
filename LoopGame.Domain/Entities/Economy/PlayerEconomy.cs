namespace LoopGame.Domain.Entities.Economy;

/// <summary>
/// Single source of truth for a player's EGP balance and salary tier.
/// Composite unique key on player_id (1:1 with Player).
/// Balance/totals are only mutated through the domain methods below;
/// EconomyService is the sole application-layer writer.
/// </summary>
public class PlayerEconomy
{
    public PlayerEconomy() { }

    public PlayerEconomy(int playerId)
    {
        PlayerId = playerId;
    }

    public int      EconomyId    { get; set; }
    public int      PlayerId     { get; set; }
    public decimal  Balance      { get; private set; } = 0m; // CHECK >= 0
    public int      SalaryTier   { get; private set; } = 1;  // 1–5
    public decimal  TotalEarned  { get; private set; } = 0m;
    public decimal  TotalSpent   { get; private set; } = 0m;
    public DateTime UpdatedAt    { get; private set; } = DateTime.UtcNow;

    // Navigation
    public Player.Player Player { get; set; } = null!;

    /// <summary>
    /// Sets the salary tier (1–5). Returns failure if out of range.
    /// </summary>
    public Result SetSalaryTier(int tier)
    {
        if (tier is < 1 or > 5)
            return Result.Failure(EconomyErrors.InvalidSalaryTier);

        SalaryTier = tier;
        Touch();
        return Result.Success();
    }

    public const decimal MaxTransactionAmount = 99_999_999.99m;

    public static decimal RoundToCurrency(decimal value)
        => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Credits the balance (amount must be > 0) and returns the ledger row
    /// to be persisted in the SAME database transaction.
    /// Only credit-type transactions (Salary, Bonus, SideTask, BugBounty) are permitted.
    /// </summary>
    public Transaction Credit(decimal amount, TransactionType type, string description, int? referenceId = null)
    {
        amount = RoundToCurrency(amount);
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Credit amount must be positive.");
        if (amount > MaxTransactionAmount)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Credit amount exceeds maximum allowable limit.");

        if (type is not (TransactionType.Salary or TransactionType.Bonus or TransactionType.SideTask or TransactionType.BugBounty))
            throw new ArgumentException($"Transaction type {type} is not a valid credit type.", nameof(type));

        Balance += amount;
        TotalEarned += amount;
        Touch();
        return BuildLedger(amount, type, description, referenceId);
    }

    /// <summary>
    /// Attempts a debit. Fails with <see cref="EconomyErrors.InsufficientBalance"/>
    /// when amount <= 0 or Balance < amount. Never makes the balance negative.
    /// Only debit-type transactions (Purchase, Penalty) are permitted.
    /// </summary>
    public Result<Transaction> TryDebit(decimal amount, TransactionType type, string description, int? referenceId = null)
    {
        amount = RoundToCurrency(amount);
        if (amount <= 0)
            return Result.Failure<Transaction>(EconomyErrors.InsufficientBalance);
        if (amount > MaxTransactionAmount)
            return Result.Failure<Transaction>(EconomyErrors.AmountExceedsMaximum);

        if (type is not (TransactionType.Purchase or TransactionType.Penalty))
            return Result.Failure<Transaction>(EconomyErrors.InvalidTransactionType);

        if (Balance < amount)
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
        amount = RoundToCurrency(amount);
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Penalty amount must be positive.");
        if (amount > MaxTransactionAmount)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Penalty amount exceeds maximum allowable limit.");

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
