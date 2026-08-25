namespace LoopGame.Infrastructure.Persistence.Configurations;

public class ConsequenceConfiguration : IEntityTypeConfiguration<Consequence>
{
    public void Configure(EntityTypeBuilder<Consequence> builder)
    {
        builder.ToTable("Consequence");
        builder.HasKey(c => c.ConsequenceId);

        builder.Property(c => c.InjectPosition)
               .HasColumnType("varchar(10)")
               .HasDefaultValue("start")
               .IsRequired();

        builder.HasCheckConstraint("CHK_Consequence_InjectPosition",
            "\"InjectPosition\" IN ('start', 'end')");

        // 1:1 with StoryBeat (beat_id is unique on Consequence side)
        builder.HasOne(c => c.Beat)
               .WithOne(b => b.Consequence)
               .HasForeignKey<Consequence>(c => c.BeatId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.BeatId)
               .IsUnique()
               .HasDatabaseName("IX_Consequence_Beat");
    }
}
