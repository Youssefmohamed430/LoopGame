namespace LoopGame.Domain.Entities.Economy;

/// <summary>
/// Immutable ledger record of all credits and debits for a player.
/// Table name is quoted in SQL because 'Transaction' is a reserved word.
/// </summary>
public class Transaction
{
    public int             TransactionId   { get; set; }
    public int             PlayerId        { get; set; }
    public decimal         Amount          { get; set; }
    public TransactionType TransactionType { get; set; }
    public string          Description     { get; set; } = string.Empty;
    public int?            ReferenceId     { get; set; }
    public decimal         BalanceAfter    { get; set; }
    public DateTime        CreatedAt       { get; set; } = DateTime.UtcNow;

    // Navigation
    public Player.Player Player { get; set; } = null!;
}
