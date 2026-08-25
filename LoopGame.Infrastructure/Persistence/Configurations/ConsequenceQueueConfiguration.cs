namespace LoopGame.Infrastructure.Persistence.Configurations;

public class ConsequenceQueueConfiguration : IEntityTypeConfiguration<ConsequenceQueue>
{
    public void Configure(EntityTypeBuilder<ConsequenceQueue> builder)
    {
        builder.ToTable("ConsequenceQueue");
        builder.HasKey(q => q.QueueId);

        builder.Property(q => q.Status)
               .HasColumnType("varchar(20)")
               .HasDefaultValue(ConsequenceStatus.pending)
               .HasConversion(
                   v => v.ToString().ToLower(),
                   v => Enum.Parse<ConsequenceStatus>(v, true))
               .IsRequired();

        builder.HasCheckConstraint("CHK_Queue_Status",
            "\"Status\" IN ('pending', 'fired', 'dismissed')");

        builder.Property(q => q.QueuedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.Property(q => q.FiredAt)
               .HasColumnType("timestamp with time zone");

        // Composite unique: a player can only have one queue entry per consequence
        builder.HasIndex(q => new { q.PlayerId, q.ConsequenceId })
               .IsUnique()
               .HasDatabaseName("UQ_Queue_Player_Consequence");

        // Composite index: shift-start pending consequence lookup
        builder.HasIndex(q => new { q.PlayerId, q.Status })
               .HasDatabaseName("IX_Queue_Player_Status");

        builder.HasOne(q => q.Player)
               .WithMany(p => p.ConsequenceQueues)
               .HasForeignKey(q => q.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.Consequence)
               .WithMany(c => c.ConsequenceQueues)
               .HasForeignKey(q => q.ConsequenceId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
