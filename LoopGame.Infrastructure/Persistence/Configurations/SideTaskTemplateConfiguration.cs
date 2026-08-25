namespace LoopGame.Infrastructure.Persistence.Configurations;

public class SideTaskTemplateConfiguration : IEntityTypeConfiguration<SideTaskTemplate>
{
    public void Configure(EntityTypeBuilder<SideTaskTemplate> builder)
    {
        builder.ToTable("SideTaskTemplate");
        builder.HasKey(t => t.TemplateId);

        builder.Property(t => t.TemplateKey)
               .HasColumnType("varchar(100)")
               .IsRequired();

        builder.HasIndex(t => t.TemplateKey).IsUnique();

        builder.Property(t => t.ConceptTag)
               .HasColumnType("varchar(50)")
               .IsRequired();

        // PlayerRank enum → string (with space for ExperiencedJunior)
        builder.Property(t => t.RankRequired)
               .HasColumnType("varchar(30)")
               .HasDefaultValue(PlayerRank.Intern)
               .HasConversion(
                   v => v == PlayerRank.ExperiencedJunior ? "Experienced Junior" : v.ToString(),
                   v => v == "Experienced Junior" ? PlayerRank.ExperiencedJunior : Enum.Parse<PlayerRank>(v));

        builder.Property(t => t.TitleTemplate)
               .HasMaxLength(300)
               .IsRequired();

        // PostgreSQL native jsonb column
        builder.Property(t => t.SlotsSchema)
               .HasColumnType("jsonb")
               .IsRequired();

        builder.Property(t => t.EgpMin)
               .HasPrecision(8, 2)
               .HasDefaultValue(500m);

        builder.Property(t => t.EgpMax)
               .HasPrecision(8, 2)
               .HasDefaultValue(3000m);

        builder.Property(t => t.CreatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");
    }
}
