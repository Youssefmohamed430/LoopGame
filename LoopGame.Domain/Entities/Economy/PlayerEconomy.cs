namespace LoopGame.Domain.Entities.Economy;

/// <summary>
/// Single source of truth for a player's EGP balance and salary tier.
/// Composite unique key on player_id (1:1 with Player).
/// </summary>
public class PlayerEconomy
{
    public int      EconomyId    { get; set; }
    public int      PlayerId     { get; set; }
    public decimal  Balance      { get; set; } = 0m; // CHECK >= 0
    public int      SalaryTier   { get; set; } = 1;  // 1–5
    public decimal  TotalEarned  { get; set; } = 0m;
    public decimal  TotalSpent   { get; set; } = 0m;
    public DateTime UpdatedAt    { get; set; } = DateTime.UtcNow;

    // Navigation
    public Player.Player Player { get; set; } = null!;
}
