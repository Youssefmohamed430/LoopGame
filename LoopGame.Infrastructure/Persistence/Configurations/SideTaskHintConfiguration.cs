namespace LoopGame.Infrastructure.Persistence.Configurations;

public class SideTaskHintConfiguration : IEntityTypeConfiguration<SideTaskHint>
{
    public void Configure(EntityTypeBuilder<SideTaskHint> builder)
    {
        builder.ToTable("SideTaskHint");
        builder.HasKey(h => h.HintId);

        builder.Property(h => h.HintLevel)
               .HasColumnType("smallint")
               .IsRequired();

        builder.HasCheckConstraint("CHK_SideTaskHint_Level",
            "\"HintLevel\" BETWEEN 1 AND 3");

        builder.Property(h => h.EgpCost)
               .HasPrecision(8, 2)
               .HasDefaultValue(0m);

        builder.Property(h => h.UnlockedAt)
               .HasColumnType("timestamp with time zone");

        builder.Property(h => h.CreatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        // Composite unique index: fast lookup of hint by level per task
        builder.HasIndex(h => new { h.SideTaskId, h.HintLevel })
               .IsUnique()
               .HasDatabaseName("IX_SideTaskHint_Task_Level");

        // 1:N with PlayerSideTask; cascade delete
        builder.HasOne(h => h.SideTask)
               .WithMany(t => t.Hints)
               .HasForeignKey(h => h.SideTaskId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
