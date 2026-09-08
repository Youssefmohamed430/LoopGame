namespace LoopGame.Domain.Entities.Economy;

/// <summary>
/// Immutable ledger record of all credits and debits for a player.
/// Table name is quoted in SQL because 'Transaction' is a reserved word.
/// </summary>
public class Transaction
{
    public int             TransactionId   { get; init; }
    public int             PlayerId        { get; init; }
    public decimal         Amount          { get; init; }
    public TransactionType TransactionType { get; init; }
    public string          Description     { get; init; } = string.Empty;
    public int?            ReferenceId     { get; init; }
    public decimal         BalanceAfter    { get; init; }
    public DateTime        CreatedAt       { get; init; } = DateTime.UtcNow;

    // Navigation
    public Player.Player Player { get; set; } = null!;
}
