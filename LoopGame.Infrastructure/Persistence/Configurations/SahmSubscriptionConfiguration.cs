namespace LoopGame.Infrastructure.Persistence.Configurations;

public class SahmSubscriptionConfiguration : IEntityTypeConfiguration<SahmSubscription>
{
    public void Configure(EntityTypeBuilder<SahmSubscription> builder)
    {
        builder.ToTable("SahmSubscription");
        builder.HasKey(s => s.SubscriptionId);

        builder.Property(s => s.Tier)
               .HasColumnType("varchar(20)")
               .HasDefaultValue("Free")
               .IsRequired();

        builder.HasCheckConstraint("CHK_Sahm_Tier",
            "tier IN ('Free','Pro','Team','Enterprise')");

        builder.Property(s => s.ActivatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.Property(s => s.DailyHintLimit)
               .HasColumnType("smallint")
               .HasDefaultValue((byte)3);

        builder.Property(s => s.HintsUsedToday)
               .HasColumnType("smallint")
               .HasDefaultValue((byte)0);

        builder.Property(s => s.LastHintReset)
               .HasColumnType("date")
               .HasDefaultValueSql("CURRENT_DATE");

        builder.HasOne(s => s.Player)
               .WithMany(p => p.SahmSubscriptions)
               .HasForeignKey(s => s.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
