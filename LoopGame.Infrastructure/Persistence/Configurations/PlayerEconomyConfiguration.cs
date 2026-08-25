namespace LoopGame.Infrastructure.Persistence.Configurations;

public class PlayerEconomyConfiguration : IEntityTypeConfiguration<PlayerEconomy>
{
    public void Configure(EntityTypeBuilder<PlayerEconomy> builder)
    {
        builder.ToTable("PlayerEconomy");
        builder.HasKey(e => e.EconomyId);

        // 1:1 with Player
        builder.HasIndex(e => e.PlayerId)
               .IsUnique();

        builder.Property(e => e.Balance)
               .HasPrecision(10, 2)
               .HasDefaultValue(0m);

        builder.HasCheckConstraint("CHK_Economy_Balance",
            "balance >= 0");

        builder.Property(e => e.SalaryTier)
               .HasDefaultValue(1);

        builder.HasCheckConstraint("CHK_Economy_SalaryTier",
            "salary_tier BETWEEN 1 AND 5");

        builder.Property(e => e.TotalEarned)
               .HasPrecision(12, 2)
               .HasDefaultValue(0m);

        builder.Property(e => e.TotalSpent)
               .HasPrecision(12, 2)
               .HasDefaultValue(0m);

        builder.Property(e => e.UpdatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.HasOne(e => e.Player)
               .WithMany(p => p.Economy)
               .HasForeignKey(e => e.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
