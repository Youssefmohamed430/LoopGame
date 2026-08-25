namespace LoopGame.Infrastructure.Persistence.Configurations;

public class PlayerSideTaskConfiguration : IEntityTypeConfiguration<PlayerSideTask>
{
    public void Configure(EntityTypeBuilder<PlayerSideTask> builder)
    {
        builder.ToTable("PlayerSideTask");
        builder.HasKey(t => t.SideTaskId);

        builder.Property(t => t.ResolvedTitle)
               .HasMaxLength(300)
               .IsRequired();

        // SideTaskStatus enum → DB lowercase string
        builder.Property(t => t.Status)
               .HasColumnType("varchar(20)")
               .HasDefaultValue(SideTaskStatus.Active)
               .HasConversion(
                   v => v.ToString().ToLower(),
                   v => Enum.Parse<SideTaskStatus>(v, true));

        builder.HasCheckConstraint("CHK_PlayerSideTask_Status",
            "status IN ('active', 'submitted', 'abandoned', 'expired')");

        // PostgreSQL native jsonb column
        builder.Property(t => t.FilledSlots)
               .HasColumnType("jsonb")
               .IsRequired();

        builder.Property(t => t.EgpReward)
               .HasPrecision(8, 2);

        builder.Property(t => t.AssignedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.Property(t => t.DeadlineAt)
               .HasColumnType("timestamp with time zone");

        builder.Property(t => t.CompletedAt)
               .HasColumnType("timestamp with time zone");

        builder.HasOne(t => t.Player)
               .WithMany(p => p.SideTasks)
               .HasForeignKey(t => t.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Template)
               .WithMany(s => s.PlayerSideTasks)
               .HasForeignKey(t => t.TemplateId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AiLog)
               .WithMany(l => l.PlayerSideTasks)
               .HasForeignKey(t => t.AiLogId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
