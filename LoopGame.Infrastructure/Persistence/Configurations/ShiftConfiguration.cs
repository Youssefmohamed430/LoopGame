namespace LoopGame.Infrastructure.Persistence.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shift");
        builder.HasKey(s => s.ShiftId);

        builder.Property(s => s.Title)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(s => s.Description)
               .HasMaxLength(1000);

        builder.Property(s => s.CreatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        // PostgreSQL native jsonb column
        builder.Property(s => s.UnlockCondition)
               .HasColumnName("unlock_condition")
               .HasColumnType("jsonb")
               .HasConversion(
                   v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                   v => v == null ? null : JsonSerializer.Deserialize<ShiftUnlockCondition>(v, JsonOptions));

        // Unique: (chapter_number, shift_number)
        builder.HasIndex(s => new { s.ChapterNumber, s.ShiftNumber })
               .IsUnique()
               .HasDatabaseName("UQ_Shift_Number");
    }
}
