namespace LoopGame.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken");
        builder.HasKey(t => t.TokenId);

        builder.Property(t => t.TokenHash)
               .HasColumnType("character(64)")
               .IsRequired();

        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.IssuedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.Property(t => t.ExpiresAt)
               .HasColumnType("timestamp with time zone")
               .IsRequired();

        builder.Property(t => t.RevokedAt)
               .HasColumnType("timestamp with time zone");

        builder.Property(t => t.UserAgent)
               .HasMaxLength(500);

        builder.Property(t => t.IpAddress)
               .HasColumnType("varchar(45)");

        // Configure FK relationship using raw property — no navigation on Domain RefreshToken
        builder.HasOne<LoopGame.Infrastructure.Identity.ApplicationUser>()
               .WithMany(u => u.RefreshTokens)
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        // Filtered composite index: active session lookup & token rotation
        builder.HasIndex(t => new { t.UserId, t.ExpiresAt })
               .HasFilter("\"RevokedAt\" IS NULL")
               .HasDatabaseName("IX_RefreshToken_User_Expiry");
    }
}
