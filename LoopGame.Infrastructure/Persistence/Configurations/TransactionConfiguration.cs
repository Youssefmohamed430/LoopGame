namespace LoopGame.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    // Static helpers: switch expressions are not allowed inside EF HasConversion lambdas (expression trees)
    private static string ToDbString(TransactionType v) => v switch
    {
        TransactionType.Salary    => "salary",
        TransactionType.Bonus     => "bonus",
        TransactionType.SideTask  => "side_task",
        TransactionType.Purchase  => "purchase",
        TransactionType.Penalty   => "penalty",
        TransactionType.BugBounty => "bug_bounty",
        _ => "bonus"
    };

    private static TransactionType FromDbString(string v) => v switch
    {
        "salary"     => TransactionType.Salary,
        "bonus"      => TransactionType.Bonus,
        "side_task"  => TransactionType.SideTask,
        "purchase"   => TransactionType.Purchase,
        "penalty"    => TransactionType.Penalty,
        "bug_bounty" => TransactionType.BugBounty,
        _ => TransactionType.Bonus
    };

    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transaction");
        builder.HasKey(t => t.TransactionId);

        // TransactionType enum → DB snake_case string (using static helpers)
        builder.Property(t => t.TransactionType)
               .HasColumnName("transaction_type")
               .HasColumnType("varchar(30)")
               .HasConversion(
                   v => ToDbString(v),
                   v => FromDbString(v));

        builder.HasCheckConstraint("CHK_Transaction_Type",
            "transaction_type IN ('salary','bonus','side_task','purchase','penalty','bug_bounty')");

        builder.Property(t => t.Amount)
               .HasPrecision(10, 2);

        builder.Property(t => t.Description)
               .HasMaxLength(500)
               .IsRequired();

        builder.Property(t => t.BalanceAfter)
               .HasPrecision(10, 2);

        builder.Property(t => t.CreatedAt)
               .HasColumnType("timestamp with time zone")
               .HasDefaultValueSql("NOW()");

        builder.HasOne(t => t.Player)
               .WithMany(p => p.Transactions)
               .HasForeignKey(t => t.PlayerId)
               .OnDelete(DeleteBehavior.Cascade);

        // Composite index: player ledger & financial history
        builder.HasIndex(t => new { t.PlayerId, t.CreatedAt })
               .HasDatabaseName("IX_Transaction_Player_Date");

        // Salary idempotency backstop: at most ONE salary row per (player, shift).
        // Filter uses ACTUAL DB column names ("transaction_type" via HasColumnName,
        // "ReferenceId" default PascalCase naming).
        builder.HasIndex(t => new { t.PlayerId, t.ReferenceId })
               .HasDatabaseName("UX_Transaction_SalaryPerShift")
               .IsUnique()
               .HasFilter("\"transaction_type\" = 'salary' AND \"ReferenceId\" IS NOT NULL");
    }
}
