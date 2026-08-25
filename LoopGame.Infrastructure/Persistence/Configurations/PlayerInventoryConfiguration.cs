namespace LoopGame.Infrastructure.Persistence.Configurations;

public class PlayerInventoryConfiguration : IEntityTypeConfiguration<PlayerInventory>
{
    public void Configure(EntityTypeBuilder<PlayerInventory> builder)
    {
        builder.ToTable("PlayerInventory");
        builder.HasKey(i => i.InventoryId);

        builder.Property(i => i.EgpPaid)
               .HasPrecision(10, 2);

        builder.Property(i => i.PurchasedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        // Composite unique: a player can only own each item once
        builder.HasIndex(i => new { i.PlayerId, i.ItemId })
               .IsUnique()
               .HasDatabaseName("UQ_PlayerInventory");

        builder.HasOne(i => i.Player)
               .WithMany(p => p.Inventory)
               .HasForeignKey(i => i.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Item)
               .WithMany(s => s.PlayerInventories)
               .HasForeignKey(i => i.ItemId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
