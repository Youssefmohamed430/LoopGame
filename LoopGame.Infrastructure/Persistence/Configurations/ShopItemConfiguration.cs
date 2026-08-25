namespace LoopGame.Infrastructure.Persistence.Configurations;

public class ShopItemConfiguration : IEntityTypeConfiguration<ShopItem>
{
    public void Configure(EntityTypeBuilder<ShopItem> builder)
    {
        builder.ToTable("ShopItem");
        builder.HasKey(i => i.ItemId);

        builder.Property(i => i.ItemKey)
               .HasColumnType("varchar(100)")
               .IsRequired();

        builder.HasIndex(i => i.ItemKey).IsUnique();

        builder.Property(i => i.DisplayName)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(i => i.Category)
               .HasColumnType("varchar(30)")
               .IsRequired();

        builder.HasCheckConstraint("CHK_ShopItem_Category",
            "\"Category\" IN ('sahm_tier','camera','desk_item','workspace')");

        builder.Property(i => i.Description)
               .HasMaxLength(500);

        builder.Property(i => i.Price)
               .HasPrecision(10, 2);

        builder.HasCheckConstraint("CHK_ShopItem_Price",
            "\"Price\" > 0");

        // PlayerRank? enum → nullable string (with space for ExperiencedJunior)
        builder.Property(i => i.RankRequired)
               .HasColumnType("varchar(30)")
               .HasConversion(
                   v => v == null ? null
                       : v == PlayerRank.ExperiencedJunior ? "Experienced Junior"
                       : v.ToString(),
                   v => v == null ? (PlayerRank?)null
                       : v == "Experienced Junior" ? PlayerRank.ExperiencedJunior
                       : Enum.Parse<PlayerRank>(v));

        builder.Property(i => i.AssetKey)
               .HasMaxLength(200);
    }
}
