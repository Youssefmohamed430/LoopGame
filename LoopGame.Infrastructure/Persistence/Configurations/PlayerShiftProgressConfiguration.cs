namespace LoopGame.Infrastructure.Persistence.Configurations;

public class PlayerShiftProgressConfiguration : IEntityTypeConfiguration<PlayerShiftProgress>
{
    // Static helpers: switch expressions are not allowed inside EF HasConversion lambdas
    private static string ToDbString(ShiftProgressStatus v) => v switch
    {
        ShiftProgressStatus.InProgress  => "in_progress",
        ShiftProgressStatus.GatePending => "gate_pending",
        ShiftProgressStatus.Completed   => "completed",
        _ => "in_progress"
    };

    private static ShiftProgressStatus FromDbString(string v) => v switch
    {
        "gate_pending" => ShiftProgressStatus.GatePending,
        "completed"    => ShiftProgressStatus.Completed,
        _              => ShiftProgressStatus.InProgress
    };

    public void Configure(EntityTypeBuilder<PlayerShiftProgress> builder)
    {
        builder.ToTable("PlayerShiftProgress");
        builder.HasKey(p => p.ProgressId);

        // ShiftProgressStatus enum → DB snake_case string (using static helpers)
        builder.Property(p => p.Status)
               .HasColumnType("varchar(20)")
               .HasDefaultValue(ShiftProgressStatus.InProgress)
               .HasConversion(
                   v => ToDbString(v),
                   v => FromDbString(v));

        builder.HasCheckConstraint("CHK_ShiftProgress_Status",
            "status IN ('in_progress', 'gate_pending', 'completed')");

        builder.Property(p => p.StartedAt)
               .HasColumnType("timestamp with time zone");

        builder.Property(p => p.CompletedAt)
               .HasColumnType("timestamp with time zone");

        builder.Property(p => p.GateAttempts)
               .HasColumnType("smallint")
               .HasDefaultValue((short)0);

        // Composite unique: one progress record per player per shift
        builder.HasIndex(p => new { p.PlayerId, p.ShiftId })
               .IsUnique()
               .HasDatabaseName("UQ_PlayerShift");

        builder.HasOne(p => p.Player)
               .WithMany(pl => pl.ShiftProgresses)
               .HasForeignKey(p => p.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Shift)
               .WithMany(s => s.ShiftProgresses)
               .HasForeignKey(p => p.ShiftId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
