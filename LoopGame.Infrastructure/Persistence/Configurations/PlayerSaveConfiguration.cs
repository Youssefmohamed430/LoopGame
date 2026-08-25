namespace LoopGame.Infrastructure.Persistence.Configurations;

public class PlayerSaveConfiguration : IEntityTypeConfiguration<PlayerSave>
{
    public void Configure(EntityTypeBuilder<PlayerSave> builder)
    {
        builder.ToTable("PlayerSave");
        builder.HasKey(s => s.SaveId);

        builder.Property(s => s.SlotNumber)
               .HasColumnType("smallint")
               .IsRequired();

        builder.HasCheckConstraint("CHK_PlayerSave_SlotNumber",
            "slot_number IN (1, 2, 3)");

        builder.Property(s => s.SaveLabel)
               .HasMaxLength(100);

        // EF Core 9 native JSON: DesktopState ↔ desktop_state column (jsonb in PostgreSQL)
        builder.OwnsOne(s => s.DesktopState, owned =>
        {
            owned.ToJson("desktop_state");
        });

        builder.Property(s => s.SavedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        // Composite unique: one save record per player per slot
        builder.HasIndex(s => new { s.PlayerId, s.SlotNumber })
               .IsUnique()
               .HasDatabaseName("UQ_PlayerSave");

        builder.HasOne(s => s.Player)
               .WithMany()
               .HasForeignKey(s => s.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
