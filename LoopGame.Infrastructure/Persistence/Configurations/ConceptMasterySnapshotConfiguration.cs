namespace LoopGame.Infrastructure.Persistence.Configurations;

public class ConceptMasterySnapshotConfiguration : IEntityTypeConfiguration<ConceptMasterySnapshot>
{
    public void Configure(EntityTypeBuilder<ConceptMasterySnapshot> builder)
    {
        builder.ToTable("ConceptMasterySnapshot");
        builder.HasKey(s => s.SnapshotId);

        builder.Property(s => s.ConceptTag)
               .HasColumnType("varchar(50)")
               .IsRequired();

        // DECIMAL(5,4): e.g. 0.7500
        builder.Property(s => s.MasteryScore)
               .HasPrecision(5, 4);

        builder.HasCheckConstraint("CHK_Mastery_Score",
            "mastery_score BETWEEN 0 AND 1");

        builder.Property(s => s.EvidenceCount)
               .HasDefaultValue(0);

        builder.Property(s => s.SnapshottedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.HasOne(s => s.Player)
               .WithMany(p => p.MasterySnapshots)
               .HasForeignKey(s => s.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Shift)
               .WithMany(sh => sh.MasterySnapshots)
               .HasForeignKey(s => s.ShiftId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
