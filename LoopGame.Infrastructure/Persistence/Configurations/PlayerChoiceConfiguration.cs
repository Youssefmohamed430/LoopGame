namespace LoopGame.Infrastructure.Persistence.Configurations;

public class PlayerChoiceConfiguration : IEntityTypeConfiguration<PlayerChoice>
{
    public void Configure(EntityTypeBuilder<PlayerChoice> builder)
    {
        builder.ToTable("PlayerChoice");
        builder.HasKey(pc => pc.PlayerChoiceId);

        // ChoiceTier enum → string
        builder.Property(pc => pc.Tier)
               .HasColumnType("varchar(20)")
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<ChoiceTier>(v));

        builder.Property(pc => pc.ChosenAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.HasOne(pc => pc.Player)
               .WithMany(p => p.PlayerChoices)
               .HasForeignKey(pc => pc.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.Beat)
               .WithMany()
               .HasForeignKey(pc => pc.BeatId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pc => pc.Choice)
               .WithMany()
               .HasForeignKey(pc => pc.ChoiceId)
               .OnDelete(DeleteBehavior.Restrict);

        // Composite index: choice replay lookup and duplicate guard
        builder.HasIndex(pc => new { pc.PlayerId, pc.BeatId })
               .HasDatabaseName("IX_Choice_Player_Beat");
    }
}
