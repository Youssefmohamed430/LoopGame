namespace LoopGame.Infrastructure.Persistence.Configurations;

public class TestCaseConfiguration : IEntityTypeConfiguration<TestCase>
{
    public void Configure(EntityTypeBuilder<TestCase> builder)
    {
        builder.ToTable("TestCase");
        builder.HasKey(t => t.TestCaseId);

        builder.Property(t => t.Description)
               .HasMaxLength(500);

        // CHECK: belongs to exactly one parent — either a PracticeTask or a PlayerSideTask
        builder.HasCheckConstraint("CHK_TestCase_Parent",
            "(\"TaskId\" IS NOT NULL AND \"SideTaskId\" IS NULL) OR " +
            "(\"TaskId\" IS NULL AND \"SideTaskId\" IS NOT NULL)");

        builder.HasOne(t => t.Task)
               .WithMany(p => p.TestCases)
               .HasForeignKey(t => t.TaskId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.SideTask)
               .WithMany(s => s.TestCases)
               .HasForeignKey(t => t.SideTaskId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
