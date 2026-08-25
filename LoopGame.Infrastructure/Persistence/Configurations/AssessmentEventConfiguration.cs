namespace LoopGame.Infrastructure.Persistence.Configurations;

public class AssessmentEventConfiguration : IEntityTypeConfiguration<AssessmentEvent>
{
    public void Configure(EntityTypeBuilder<AssessmentEvent> builder)
    {
        builder.ToTable("AssessmentEvent");

        // BIGINT primary key for high-volume inserts
        builder.HasKey(e => e.EventId);
        builder.Property(e => e.EventId)
               .UseIdentityByDefaultColumn();

        builder.Property(e => e.EventType)
               .HasColumnType("varchar(50)")
               .IsRequired();

        builder.HasCheckConstraint("CHK_Assessment_EventType",
            "event_type IN ('choice_submission','practice_attempt','hint_request'," +
            "'side_task_submission','desktop_interaction','consequence_trigger'," +
            "'gate_cleared','shift_completed')");

        builder.Property(e => e.ConceptTag)
               .HasColumnType("varchar(50)");

        builder.Property(e => e.Tier)
               .HasColumnType("varchar(20)");

        // PostgreSQL native jsonb column
        builder.Property(e => e.Payload)
               .HasColumnType("jsonb");

        builder.Property(e => e.RecordedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.HasOne(e => e.Player)
               .WithMany(p => p.AssessmentEvents)
               .HasForeignKey(e => e.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        // Composite index: AI weakest concept calculation
        builder.HasIndex(e => new { e.PlayerId, e.EventType, e.RecordedAt })
               .HasDatabaseName("IX_Assessment_Player_Type");
    }
}
