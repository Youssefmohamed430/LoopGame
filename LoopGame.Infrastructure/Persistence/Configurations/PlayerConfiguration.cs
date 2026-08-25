namespace LoopGame.Infrastructure.Persistence.Configurations;

public class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Player");
        builder.HasKey(p => p.PlayerId);

        builder.Property(p => p.StudentIdHash)
               .HasColumnType("character(64)")
               .IsRequired();

        builder.HasIndex(p => p.StudentIdHash).IsUnique();

        // PlayerRank enum → string (with space for ExperiencedJunior)
        builder.Property(p => p.Rank)
               .HasColumnType("varchar(30)")
               .HasDefaultValue(PlayerRank.Intern)
               .HasConversion(
                   v => v == PlayerRank.ExperiencedJunior ? "Experienced Junior" : v.ToString(),
                   v => v == "Experienced Junior" ? PlayerRank.ExperiencedJunior : Enum.Parse<PlayerRank>(v));

        builder.Property(p => p.CreatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.Property(p => p.DeletedAt)
               .HasColumnType("timestamp with time zone");

        // Player.UserId → ApplicationUser 1:1 is configured in ApplicationDbContext
        // to avoid cross-assembly navigation references.

        // Unique index: fast 1:1 user → player lookup
        builder.HasIndex(p => p.PlayerId)
               .IsUnique()
               .HasDatabaseName("IX_Player_User");

        builder.HasOne(p => p.CurrentShift)
               .WithMany()
               .HasForeignKey(p => p.CurrentShiftId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        // Global soft-delete filter
        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
