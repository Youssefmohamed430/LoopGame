namespace LoopGame.Infrastructure.Persistence.Configurations;

public class ChoiceConfiguration : IEntityTypeConfiguration<Choice>
{
    public void Configure(EntityTypeBuilder<Choice> builder)
    {
        builder.ToTable("Choice");
        builder.HasKey(c => c.ChoiceId);

        builder.Property(c => c.ChoiceIndex)
               .HasColumnType("smallint")
               .IsRequired();

        builder.Property(c => c.ChoiceText)
               .HasMaxLength(500)
               .IsRequired();

        // ChoiceTier enum → string
        builder.Property(c => c.Tier)
               .HasColumnType("varchar(20)")
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<ChoiceTier>(v));

        builder.Property(c => c.ImmediateFeedback)
               .HasMaxLength(500);

        builder.HasCheckConstraint("CHK_Choice_Index",
            "choice_index BETWEEN 1 AND 4");

        // Unique: one choice per index per beat
        builder.HasIndex(c => new { c.BeatId, c.ChoiceIndex })
               .IsUnique()
               .HasDatabaseName("UQ_Choice_Beat_Index");

        builder.HasOne(c => c.Beat)
               .WithMany(b => b.Choices)
               .HasForeignKey(c => c.BeatId)
               .OnDelete(DeleteBehavior.Cascade);

        // Optional FK to Consequence
        builder.HasOne(c => c.Consequence)
               .WithMany()
               .HasForeignKey(c => c.ConsequenceId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
