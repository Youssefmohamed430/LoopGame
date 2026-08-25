namespace LoopGame.Domain.Entities.Audit;

/// <summary>
/// Security audit log tracking admin and super-admin actions.
/// audit_id is BIGINT (long) to support high-volume audit trails.
/// Both user_id and player_id are nullable (action may be system-generated).
/// ApplicationUser navigation lives in Infrastructure.
/// </summary>
public class AuditLog
{
    public long     AuditId     { get; set; }
    public int?     UserId      { get; set; }  // raw FK → ApplicationUser
    public int?     PlayerId    { get; set; }
    public string   Action      { get; set; } = string.Empty;
    public string?  EntityType  { get; set; }
    public int?     EntityId    { get; set; }
    public string?  OldValue    { get; set; }
    public string?  NewValue    { get; set; }
    public string?  IpAddress   { get; set; }
    public string?  UserAgent   { get; set; }
    public DateTime OccurredAt  { get; set; } = DateTime.UtcNow;

    // Navigation
    public Player.Player? Player { get; set; }
}
