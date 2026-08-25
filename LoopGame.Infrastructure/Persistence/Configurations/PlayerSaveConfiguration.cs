namespace LoopGame.Infrastructure.Persistence.Configurations;

public class PlayerSaveConfiguration : IEntityTypeConfiguration<PlayerSave>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<PlayerSave> builder)
    {
        builder.ToTable("PlayerSave");
        builder.HasKey(s => s.SaveId);

        builder.Property(s => s.SlotNumber)
               .HasColumnType("smallint")
               .IsRequired();

        builder.HasCheckConstraint("CHK_PlayerSave_SlotNumber",
            "\"SlotNumber\" IN (1, 2, 3)");

        builder.Property(s => s.SaveLabel)
               .HasMaxLength(100);

        // PostgreSQL native jsonb column
        builder.Property(s => s.DesktopState)
               .HasColumnName("desktop_state")
               .HasColumnType("jsonb")
               .HasConversion(
                   v => v == null ? null : JsonSerializer.Serialize(v, JsonOptions),
                   v => v == null ? null! : JsonSerializer.Deserialize<DesktopState>(v, JsonOptions)!);

        builder.Property(s => s.SavedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        // Composite unique: one save record per player per slot
        builder.HasIndex(s => new { s.PlayerId, s.SlotNumber })
               .IsUnique()
               .HasDatabaseName("UQ_PlayerSave");

        builder.HasOne(s => s.Player)
               .WithMany(p => p.PlayerSaves)
               .HasForeignKey(s => s.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
