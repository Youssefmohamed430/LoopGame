namespace LoopGame.Infrastructure.Persistence.Configurations;

public class PracticeAttemptConfiguration : IEntityTypeConfiguration<PracticeAttempt>
{
    public void Configure(EntityTypeBuilder<PracticeAttempt> builder)
    {
        builder.ToTable("PracticeAttempt");
        builder.HasKey(a => a.AttemptId);

        // ChoiceTier enum → string
        builder.Property(a => a.Tier)
               .HasColumnType("varchar(20)")
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<ChoiceTier>(v));

        builder.HasCheckConstraint("CHK_PracticeAttempt_Tier",
            "tier IN ('Ideal', 'Acceptable', 'Debt', 'Mistake')");

        // PostgreSQL native jsonb column
        builder.Property(a => a.TestResults)
               .HasColumnType("jsonb")
               .IsRequired();

        builder.Property(a => a.SubmittedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.HasOne(a => a.Player)
               .WithMany(p => p.PracticeAttempts)
               .HasForeignKey(a => a.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Task)
               .WithMany(t => t.Attempts)
               .HasForeignKey(a => a.TaskId)
               .OnDelete(DeleteBehavior.Restrict);

        // Composite index: anti-struggle detector & hint logic
        builder.HasIndex(a => new { a.PlayerId, a.TaskId, a.SubmittedAt })
               .HasDatabaseName("IX_Attempt_Player_Task");
    }
}
