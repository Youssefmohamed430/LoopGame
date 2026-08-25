namespace LoopGame.Domain.Entities.Identity;

public class ClassCode
{
    public int ClassCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int? InstructorId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation property
    public ICollection<Player.Player> Players { get; set; } = new List<Player.Player>();
}
