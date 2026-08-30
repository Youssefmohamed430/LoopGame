using LoopGame.Domain.Enums.AuthModule;
using Microsoft.AspNetCore.Identity;

namespace LoopGame.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<int>
{
    public string?    DisplayName { get; set; }
    public bool       IsActive    { get; set; } = true;
    public DateTime   CreatedAt   { get; set; } = DateTime.UtcNow;
    public DateTime?  DeletedAt   { get; set; }
    public Roles Role { get; set; }

    // Navigation
    public Player?                   Player        { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
