namespace LoopGame.Infrastructure.Persistence.Configurations;

public class PracticeTaskConfiguration : IEntityTypeConfiguration<PracticeTask>
{
    public void Configure(EntityTypeBuilder<PracticeTask> builder)
    {
        builder.ToTable("PracticeTask");
        builder.HasKey(t => t.TaskId);

        builder.Property(t => t.TaskOrder)
               .HasColumnType("smallint")
               .IsRequired();

        builder.Property(t => t.Title)
               .HasMaxLength(200)
               .IsRequired();

        builder.Property(t => t.ConceptTag)
               .HasColumnType("varchar(50)")
               .IsRequired();

        builder.Property(t => t.Difficulty)
               .HasColumnType("varchar(20)")
               .HasDefaultValue("Standard");

        builder.HasCheckConstraint("CHK_PracticeTask_Difficulty",
            "difficulty IN ('SpacedRetrieval', 'Standard', 'Challenge')");

        builder.Property(t => t.MaxAttempts)
               .HasColumnType("smallint")
               .HasDefaultValue((short)0);

        builder.Property(t => t.EgpReward)
               .HasPrecision(8, 2)
               .HasDefaultValue(0m);

        builder.Property(t => t.CreatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.HasOne(t => t.Shift)
               .WithMany(s => s.PracticeTasks)
               .HasForeignKey(t => t.ShiftId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
