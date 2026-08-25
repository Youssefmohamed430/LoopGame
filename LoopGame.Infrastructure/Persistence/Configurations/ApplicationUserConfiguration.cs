namespace LoopGame.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUser");

        builder.Property(u => u.DisplayName)
               .HasMaxLength(100);

        builder.Property(u => u.CreatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.Property(u => u.DeletedAt)
               .HasColumnType("timestamp with time zone");

        // Global soft-delete filter
        builder.HasQueryFilter(u => u.DeletedAt == null);

        // Indexes (email/username indexes are already created by IdentityDbContext)
    }
}
