namespace LoopGame.Infrastructure.Persistence.Configurations;

public class PlayerSaveConfiguration : IEntityTypeConfiguration<PlayerSave>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<PlayerSave> builder)
    {
        builder.ToTable("PlayerSave");
        builder.HasKey(s => s.SaveId);

        builder.Property(s => s.SaveLabel)
               .HasMaxLength(100);

        builder.Property(s => s.SavedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.HasOne(s => s.Player)
               .WithMany(p => p.PlayerSaves)
               .HasForeignKey(s => s.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Beat)
            .WithMany(b => b.PlayerSaves)
            .HasForeignKey(p => p.BeatId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
