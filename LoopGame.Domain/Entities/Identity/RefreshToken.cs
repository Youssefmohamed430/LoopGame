namespace LoopGame.Domain.Entities.Identity;

/// <summary>
/// Stores SHA-256 hashed JWT refresh tokens for active user sessions.
/// </summary>
public class RefreshToken
{
    public int       TokenId    { get; set; }
    public int       UserId     { get; set; }
    public string    TokenHash  { get; set; } = string.Empty; // CHAR(64) SHA-256 hex
    public DateTime  IssuedAt   { get; set; } = DateTime.UtcNow;
    public DateTime  ExpiresAt  { get; set; }
    public DateTime? RevokedAt  { get; set; }
    public string?   UserAgent  { get; set; }
    public string?   IpAddress  { get; set; }

    // Navigation — ApplicationUser lives in Infrastructure
}
