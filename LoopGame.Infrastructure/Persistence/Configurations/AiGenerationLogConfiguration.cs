namespace LoopGame.Infrastructure.Persistence.Configurations;

public class AiGenerationLogConfiguration : IEntityTypeConfiguration<AiGenerationLog>
{
    public void Configure(EntityTypeBuilder<AiGenerationLog> builder)
    {
        builder.ToTable("AiGenerationLog");
        builder.HasKey(l => l.LogId);

        builder.Property(l => l.ModelName)
               .HasMaxLength(100)
               .IsRequired();

        // PostgreSQL native jsonb column
        builder.Property(l => l.ParsedSlots)
               .HasColumnType("jsonb");

        // DECIMAL(8,6) for cost: e.g. 0.000123
        builder.Property(l => l.EstimatedCostUsd)
               .HasPrecision(8, 6);

        builder.Property(l => l.ValidationError)
               .HasMaxLength(500);

        builder.Property(l => l.CreatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.Property(l => l.ExpiresAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW() + INTERVAL '2 years'");

        builder.HasOne(l => l.Player)
               .WithMany(p => p.AiGenerationLogs)
               .HasForeignKey(l => l.PlayerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Template)
               .WithMany(t => t.AiGenerationLogs)
               .HasForeignKey(l => l.TemplateId)
               .OnDelete(DeleteBehavior.Restrict);

        // Index: scheduled 2-year retention cleanup job
        builder.HasIndex(l => l.ExpiresAt)
               .HasDatabaseName("IX_AiLog_Expiry");
    }
}
