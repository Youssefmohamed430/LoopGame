namespace LoopGame.Infrastructure.Persistence.Configurations;

public class SideTaskSubmissionConfiguration : IEntityTypeConfiguration<SideTaskSubmission>
{
    public void Configure(EntityTypeBuilder<SideTaskSubmission> builder)
    {
        builder.ToTable("SideTaskSubmission");
        builder.HasKey(s => s.SubmissionId);

        // ChoiceTier enum → string
        builder.Property(s => s.Tier)
               .HasColumnType("varchar(20)")
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<ChoiceTier>(v));

        builder.HasCheckConstraint("CHK_SideTaskSubmission_Tier",
            "\"Tier\" IN ('Ideal', 'Acceptable', 'Debt', 'Mistake')");

        // PostgreSQL native jsonb column
        builder.Property(s => s.TestResults)
               .HasColumnType("jsonb")
               .IsRequired();

        builder.Property(s => s.SahmHintsUsed)
               .HasColumnType("smallint")
               .HasDefaultValue((byte)0);

        builder.Property(s => s.EgpEarned)
               .HasPrecision(8, 2)
               .HasDefaultValue(0m);

        builder.Property(s => s.SubmittedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.HasOne(s => s.SideTask)
               .WithMany(t => t.Submissions)
               .HasForeignKey(s => s.SideTaskId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Player)
               .WithMany()
               .HasForeignKey(s => s.PlayerId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
