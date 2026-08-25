namespace LoopGame.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the SHIFT game.
/// Extends IdentityDbContext to integrate ASP.NET Core Identity.
/// All 25 tables reside in the default dbo schema.
/// Entity configurations are loaded automatically via ApplyConfigurationsFromAssembly.
/// </summary>
public class AppDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, int,
        IdentityUserClaim<int>, IdentityUserRole<int>, IdentityUserLogin<int>,
        IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // ── Identity / Access ────────────────────────────────────────
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // ── Content & Narrative ──────────────────────────────────────
    public DbSet<Shift>            Shifts         => Set<Shift>();
    public DbSet<StoryBeat>        StoryBeats     => Set<StoryBeat>();
    public DbSet<Choice>           Choices        => Set<Choice>();
    public DbSet<Consequence>      Consequences   => Set<Consequence>();
    public DbSet<PracticeTask>     PracticeTasks  => Set<PracticeTask>();
    public DbSet<TestCase>         TestCases      => Set<TestCase>();
    public DbSet<SideTaskTemplate> SideTaskTemplates => Set<SideTaskTemplate>();

    // ── Runtime Player State ─────────────────────────────────────
    public DbSet<Player>                Players              => Set<Player>();
    public DbSet<PlayerSave>            PlayerSaves          => Set<PlayerSave>();
    public DbSet<PlayerShiftProgress>   PlayerShiftProgresses => Set<PlayerShiftProgress>();
    public DbSet<PlayerChoice>          PlayerChoices        => Set<PlayerChoice>();
    public DbSet<PracticeAttempt>       PracticeAttempts     => Set<PracticeAttempt>();
    public DbSet<ConsequenceQueue>      ConsequenceQueues    => Set<ConsequenceQueue>();
    public DbSet<PlayerSideTask>        PlayerSideTasks      => Set<PlayerSideTask>();
    public DbSet<SideTaskSubmission>    SideTaskSubmissions  => Set<SideTaskSubmission>();
    public DbSet<SideTaskHint>          SideTaskHints        => Set<SideTaskHint>();

    // ── Economy & Finance ────────────────────────────────────────
    public DbSet<PlayerEconomy>   PlayerEconomies  => Set<PlayerEconomy>();
    public DbSet<Transaction>     Transactions     => Set<Transaction>();
    public DbSet<ShopItem>        ShopItems        => Set<ShopItem>();
    public DbSet<PlayerInventory> PlayerInventories => Set<PlayerInventory>();
    public DbSet<SahmSubscription> SahmSubscriptions => Set<SahmSubscription>();

    // ── Stealth Assessment ───────────────────────────────────────
    public DbSet<AssessmentEvent>        AssessmentEvents       => Set<AssessmentEvent>();
    public DbSet<ConceptMasterySnapshot> ConceptMasterySnapshots => Set<ConceptMasterySnapshot>();

    // ── AI Pipeline & Audit ──────────────────────────────────────
    public DbSet<AiGenerationLog> AiGenerationLogs => Set<AiGenerationLog>();
    public DbSet<AuditLog>        AuditLogs         => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // All tables live in the default public schema for PostgreSQL
        modelBuilder.HasDefaultSchema("public");

        // Rename ASP.NET Identity tables to match the ERD schema
        modelBuilder.Entity<ApplicationUser>()     .ToTable("ApplicationUser");
        modelBuilder.Entity<ApplicationRole>()     .ToTable("ApplicationRole");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("ApplicationUserRole");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("ApplicationUserClaim");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("ApplicationUserLogin");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("ApplicationUserToken");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("ApplicationRoleClaim");

        // ── Cross-boundary FK configurations ─────────────────────────
        // These relationships cross the Domain/Infrastructure boundary
        // (ApplicationUser lives in Infrastructure, entities in Domain)


        // Player.UserId → ApplicationUser (1:1)
        // Player has no navigation property to ApplicationUser (Domain isolation)
        modelBuilder.Entity<Player>()
            .HasOne<ApplicationUser>()
            .WithOne(u => u.Player)
            .HasForeignKey<Player>(p => p.PlayerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Load all IEntityTypeConfiguration<T> from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
