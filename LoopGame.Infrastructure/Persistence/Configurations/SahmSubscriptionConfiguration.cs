namespace LoopGame.Infrastructure.Persistence.Configurations;

public class SahmSubscriptionConfiguration : IEntityTypeConfiguration<SahmSubscription>
{
    // Static helpers: switch expressions are not allowed inside EF HasConversion lambdas (expression trees)
    private static string ToDbString(SahmTier v) => v switch
    {
        SahmTier.Pro        => "Pro",
        SahmTier.Team       => "Team",
        SahmTier.Enterprise => "Enterprise",
        _ => "Free"
    };

    private static SahmTier FromDbString(string v) => v switch
    {
        "Pro"        => SahmTier.Pro,
        "Team"       => SahmTier.Team,
        "Enterprise" => SahmTier.Enterprise,
        _ => SahmTier.Free
    };

    public void Configure(EntityTypeBuilder<SahmSubscription> builder)
    {
        builder.ToTable("SahmSubscription");
        builder.HasKey(s => s.SubscriptionId);

        // SahmTier enum → DB string (using static helpers); store values unchanged
        builder.Property(s => s.Tier)
               .HasColumnType("varchar(20)")
               .HasConversion(
                   v => ToDbString(v),
                   v => FromDbString(v))
               .HasDefaultValue(SahmTier.Free)
               .IsRequired();

        builder.HasCheckConstraint("CHK_Sahm_Tier",
            "\"Tier\" IN ('Free','Pro','Team','Enterprise')");

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
