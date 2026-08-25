namespace LoopGame.Infrastructure.Persistence.Configurations;

public class TestCaseConfiguration : IEntityTypeConfiguration<TestCase>
{
    public void Configure(EntityTypeBuilder<TestCase> builder)
    {
        builder.ToTable("TestCase");
        builder.HasKey(t => t.TestCaseId);

        builder.Property(t => t.Description)
               .HasMaxLength(500);

        // CHECK: belongs to exactly one parent (task XOR template)
        builder.HasCheckConstraint("CHK_TestCase_Parent",
            "(task_id IS NOT NULL AND template_id IS NULL) OR " +
            "(task_id IS NULL AND template_id IS NOT NULL)");

        builder.HasOne(t => t.Task)
               .WithMany(p => p.TestCases)
               .HasForeignKey(t => t.TaskId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Template)
               .WithMany(s => s.TestCases)
               .HasForeignKey(t => t.TemplateId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
