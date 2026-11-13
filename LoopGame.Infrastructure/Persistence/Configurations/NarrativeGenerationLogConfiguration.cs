namespace LoopGame.Infrastructure.Persistence.Configurations;

public sealed class NarrativeGenerationLogConfiguration : IEntityTypeConfiguration<NarrativeGenerationLog>
{
    public void Configure(EntityTypeBuilder<NarrativeGenerationLog> builder)
    {
        builder.ToTable("NarrativeGenerationLog");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.OperationType)
               .HasMaxLength(NarrativeGenerationLog.MaxOperationTypeLength)
               .IsRequired();

        builder.Property(l => l.ModelName)
               .HasMaxLength(NarrativeGenerationLog.MaxModelNameLength)
               .IsRequired();

        builder.Property(l => l.ResponseTimeMs)
               .IsRequired();

        builder.Property(l => l.ValidationPassed)
               .IsRequired();

        builder.Property(l => l.RepairAttempted)
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(l => l.UsedFallback)
               .HasDefaultValue(false)
               .IsRequired();

        builder.Property(l => l.FailureReason)
               .HasMaxLength(NarrativeGenerationLog.MaxFailureReasonLength);

        builder.Property(l => l.CreatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.HasOne(l => l.Player)
               .WithMany(p => p.NarrativeGenerationLogs)
               .HasForeignKey(l => l.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Shift)
               .WithMany()
               .HasForeignKey(l => l.ShiftId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.PlayerId, l.CreatedAt })
               .HasDatabaseName("IX_NarrativeGenerationLog_Player_CreatedAt");

        // Supports 90-day retention pruning queries filtering on CreatedAt
        builder.HasIndex(l => l.CreatedAt)
               .HasDatabaseName("IX_NarrativeGenerationLog_CreatedAt");
    }
}
