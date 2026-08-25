namespace LoopGame.Domain.Entities.Player;

public class Player
{
    public int PlayerId { get; set; }
    public string StudentIdHash { get; set; } = string.Empty; // SHA-256 CHAR(64)
    public PlayerRank Rank { get; set; } = PlayerRank.Intern;
    public int? CurrentShiftId { get; set; }
    public int TotalPlayTimeSec { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    // Navigations
    public Shift? CurrentShift { get; set; }
    public ICollection<PlayerSave> PlayerSaves { get; set; } = [];
    public ICollection<PlayerChoice> PlayerChoices { get; set; } = [];
    public ICollection<PlayerShiftProgress> ShiftProgresses { get; set; } = [];
    public ICollection<ConsequenceQueue> ConsequenceQueues { get; set; } = [];
    public ICollection<Code.PracticeAttempt> PracticeAttempts { get; set; } = [];
    public ICollection<SideTask.PlayerSideTask> SideTasks { get; set; } = [];
    public ICollection<Economy.PlayerEconomy> Economy { get; set; } = [];
    public ICollection<Economy.Transaction> Transactions { get; set; } = [];
    public ICollection<Economy.PlayerInventory> Inventory { get; set; } = [];
    public ICollection<Economy.SahmSubscription> SahmSubscriptions { get; set; } = [];
    public ICollection<Assessment.AssessmentEvent> AssessmentEvents { get; set; } = [];
    public ICollection<Assessment.ConceptMasterySnapshot> MasterySnapshots { get; set; } = [];
    public ICollection<Audit.AiGenerationLog> AiGenerationLogs { get; set; } = [];
}
