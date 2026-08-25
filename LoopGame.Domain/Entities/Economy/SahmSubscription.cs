namespace LoopGame.Domain.Entities.Economy;

/// <summary>
/// History and active tier of a player's Sahm AI assistant subscription.
/// Tier: Free | Pro | Team | Enterprise.
/// </summary>
public class SahmSubscription
{
    public int      SubscriptionId   { get; set; }
    public int      PlayerId         { get; set; }

    /// <summary>Free | Pro | Team | Enterprise</summary>
    public string   Tier             { get; set; } = "Free";

    public DateTime ActivatedAt      { get; set; } = DateTime.UtcNow;
    public byte     DailyHintLimit   { get; set; } = 3;
    public byte     HintsUsedToday   { get; set; } = 0;
    public DateOnly LastHintReset    { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    // Navigation
    public Player.Player Player { get; set; } = null!;
}
