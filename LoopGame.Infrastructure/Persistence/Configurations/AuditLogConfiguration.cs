namespace LoopGame.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLog");

        // BIGINT primary key for high-volume audit entries
        builder.HasKey(a => a.AuditId);
        builder.Property(a => a.AuditId)
               .UseIdentityByDefaultColumn();

        builder.Property(a => a.Action)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(a => a.EntityType)
               .HasColumnType("varchar(50)");

        builder.Property(a => a.IpAddress)
               .HasColumnType("varchar(45)");

        builder.Property(a => a.UserAgent)
               .HasMaxLength(500);

        builder.Property(a => a.OccurredAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        // Both FKs are nullable (system-generated actions have no user/player)
        // UserId FK: no navigation on AuditLog — ApplicationUser lives in Infrastructure
        builder.HasOne<LoopGame.Infrastructure.Identity.ApplicationUser>()
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Player)
               .WithMany()
               .HasForeignKey(a => a.PlayerId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
