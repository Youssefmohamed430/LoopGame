namespace LoopGame.Infrastructure.Persistence.Configurations;

public class StoryBeatConfiguration : IEntityTypeConfiguration<StoryBeat>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<StoryBeat> builder)
    {
        builder.ToTable("StoryBeat");
        builder.HasKey(b => b.BeatId);

        builder.Property(b => b.BeatKey)
               .HasColumnType("varchar(100)")
               .IsRequired();

        builder.HasIndex(b => b.BeatKey).IsUnique();

        // BeatType enum → string
        builder.Property(b => b.BeatType)
               .HasColumnName("beat_type")
               .HasColumnType("varchar(20)")
               .HasDefaultValue(BeatType.Narrative)
               .HasConversion(
                   v => v == BeatType.Narrative ? "narrative" : "consequence",
                   v => v == "narrative" ? BeatType.Narrative : BeatType.Consequence);

        // App enum → string
        builder.Property(b => b.App)
               .HasColumnName("app")
               .HasColumnType("varchar(50)")
               .HasConversion(
                   v => v.ToString(),
                   v => Enum.Parse<BeatApp>(v));

        builder.Property(b => b.SenderName)
               .HasMaxLength(100);

        // content_json: JSON column via HasConversion (jsonb in PostgreSQL)
        builder.Property(b => b.ContentJson)
               .HasColumnName("content_json")
               .HasColumnType("jsonb")
               .HasConversion(
                   v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                   v => v == null ? null : JsonSerializer.Deserialize<StoryBeatContent>(v, JsonOptions)!);

        // desktop_event: nullable JSON via HasConversion (jsonb in PostgreSQL)
        builder.Property(b => b.DesktopEvent)
               .HasColumnName("desktop_event")
               .HasColumnType("jsonb")
               .HasConversion(
                   v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                   v => v == null ? null : JsonSerializer.Deserialize<DesktopEvent>(v, JsonOptions));

        builder.Property(b => b.DelaySeconds)
               .HasColumnType("decimal(5,1)")
               .HasDefaultValue(0m);

        builder.Property(b => b.CreatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        // CHECK: narrative beats must have sequence_order; consequence beats must not
        builder.HasCheckConstraint("CHK_Beat_SequenceOrder",
            "(beat_type = 'narrative' AND \"SequenceOrder\" IS NOT NULL) OR " +
            "(beat_type = 'consequence' AND \"SequenceOrder\" IS NULL)");

        builder.HasOne(b => b.Shift)
               .WithMany(s => s.StoryBeats)
               .HasForeignKey(b => b.ShiftId)
               .OnDelete(DeleteBehavior.Restrict);

        // Composite index for sequential narrative beat streaming
        builder.HasIndex(b => new { b.ShiftId, b.BeatType, b.SequenceOrder })
               .HasDatabaseName("IX_Beat_Shift_Seq");
    }
}
