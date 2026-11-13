using LoopGame.Application.Dtos.NarrativeDtos;
using LoopGame.Application.Mapping;
using LoopGame.Application.Services.LearningAndContentServices;
using Mapster;

namespace LoopGame.Tests.Services;

public class NarrativeServiceTests : IDisposable
{
    private static readonly Lazy<bool> MapsterInitialized = new(() =>
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(MapsterConfiguration).Assembly);
        return true;
    });

    private readonly AppDbContext _db;
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly NarrativeService _sut;

    public NarrativeServiceTests()
    {
        _ = MapsterInitialized.Value;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _uow.Setup(u => u.GetRepository<Player>()).Returns(new BaseRepository<Player>(_db));
        _uow.Setup(u => u.GetRepository<Shift>()).Returns(new BaseRepository<Shift>(_db));
        _uow.Setup(u => u.GetRepository<StoryBeat>()).Returns(new BaseRepository<StoryBeat>(_db));
        _uow.Setup(u => u.GetRepository<Choice>()).Returns(new BaseRepository<Choice>(_db));
        _uow.Setup(u => u.GetRepository<Consequence>()).Returns(new BaseRepository<Consequence>(_db));
        _uow.Setup(u => u.GetRepository<ConsequenceQueue>()).Returns(new BaseRepository<ConsequenceQueue>(_db));
        _uow.Setup(u => u.GetRepository<PlayerSave>()).Returns(new BaseRepository<PlayerSave>(_db));
        _uow.Setup(u => u.GetRepository<PlayerShiftProgress>()).Returns(new BaseRepository<PlayerShiftProgress>(_db));

        _uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => _db.SaveChangesAsync(ct));
        _uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _sut = new NarrativeService(_uow.Object);
    }

    private static StoryBeatContent Content(string text) => new(text, null, null, null);

    private async Task<(Shift shift1, Shift shift2, Player p1, Player p2)> SeedShiftsAndPlayersAsync()
    {
        var shift1 = new Shift
        {
            ShiftId = 1,
            ChapterNumber = 1,
            ShiftNumber = 1,
            Title = "Shift 1",
            ConceptTag = Concept.Variables
        };
        var shift2 = new Shift
        {
            ShiftId = 2,
            ChapterNumber = 1,
            ShiftNumber = 2,
            Title = "Shift 2",
            ConceptTag = Concept.Loops
        };
        var p1 = new Player { PlayerId = 1, PlayerName = "PlayerOne", CurrentShiftId = 1 };
        var p2 = new Player { PlayerId = 2, PlayerName = "PlayerTwo", CurrentShiftId = 1 };

        _db.Shifts.AddRange(shift1, shift2);
        _db.Players.AddRange(p1, p2);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        return (shift1, shift2, p1, p2);
    }

    // ── StartShift ───────────────────────────────────────────────────────────

    [Fact]
    public async Task StartShift_FirstStart_CombinesGlobalAndOwnAiBeats_ExcludesOtherPlayerBeats()
    {
        await SeedShiftsAndPlayersAsync();

        _db.StoryBeats.AddRange(
            new StoryBeat
            {
                BeatId = 10,
                ShiftId = 1,
                BeatKey = "s1_global_1",
                BeatType = BeatType.Narrative,
                SequenceOrder = 1,
                App = BeatApp.WhatsUpp,
                ContentJson = Content("Global beat"),
                PlayerId = null,
                IsAiGenerated = false
            },
            new StoryBeat
            {
                BeatId = 11,
                ShiftId = 1,
                BeatKey = "s1_p1_ai_2",
                BeatType = BeatType.Narrative,
                SequenceOrder = 2,
                App = BeatApp.WhatsUpp,
                ContentJson = Content("Player 1 private beat"),
                PlayerId = 1,
                IsAiGenerated = true
            },
            new StoryBeat
            {
                BeatId = 12,
                ShiftId = 1,
                BeatKey = "s1_p2_ai_3",
                BeatType = BeatType.Narrative,
                SequenceOrder = 3,
                App = BeatApp.WhatsUpp,
                ContentJson = Content("Player 2 private beat"),
                PlayerId = 2,
                IsAiGenerated = true
            });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.StartShift(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.ShiftId);
        Assert.Equal([10, 11], result.Value.Beats.Select(b => b.BeatId).ToList());
        Assert.DoesNotContain(result.Value.Beats, b => b.BeatId == 12);

        var progress = await _db.PlayerShiftProgresses.SingleAsync(p => p.PlayerId == 1 && p.ShiftId == 1);
        Assert.Equal(ShiftProgressStatus.InProgress, progress.Status);

        var save = await _db.PlayerSaves.SingleAsync(s => s.PlayerId == 1);
        Assert.Equal(10, save.BeatId);
    }

    [Fact]
    public async Task StartShift_FirstStart_FiresOwnPendingConsequences_AndExcludesOtherPlayerConsequences()
    {
        await SeedShiftsAndPlayersAsync();

        var narrativeBeat = new StoryBeat
        {
            BeatId = 10,
            ShiftId = 1,
            BeatKey = "s1_nav",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("Narrative")
        };
        var globalConseqBeat = new StoryBeat
        {
            BeatId = 11,
            ShiftId = 1,
            BeatKey = "s1_conseq_global",
            BeatType = BeatType.Consequence,
            SequenceOrder = null,
            App = BeatApp.MailLoop,
            ContentJson = Content("Global start consequence"),
            PlayerId = null,
            IsAiGenerated = false
        };
        var ownAiConseqBeat = new StoryBeat
        {
            BeatId = 12,
            ShiftId = 1,
            BeatKey = "s1_conseq_p1",
            BeatType = BeatType.Consequence,
            SequenceOrder = null,
            App = BeatApp.MailLoop,
            ContentJson = Content("Player 1 end consequence"),
            PlayerId = 1,
            IsAiGenerated = true
        };
        var otherPlayerAiConseqBeat = new StoryBeat
        {
            BeatId = 13,
            ShiftId = 1,
            BeatKey = "s1_conseq_p2",
            BeatType = BeatType.Consequence,
            SequenceOrder = null,
            App = BeatApp.MailLoop,
            ContentJson = Content("Player 2 private consequence"),
            PlayerId = 2,
            IsAiGenerated = true
        };
        _db.StoryBeats.AddRange(narrativeBeat, globalConseqBeat, ownAiConseqBeat, otherPlayerAiConseqBeat);

        _db.Consequences.AddRange(
            new Consequence { ConsequenceId = 101, BeatId = 11, InjectPosition = "start" },
            new Consequence { ConsequenceId = 102, BeatId = 12, InjectPosition = "end" },
            new Consequence { ConsequenceId = 103, BeatId = 13, InjectPosition = "start" });

        _db.ConsequenceQueues.AddRange(
            new ConsequenceQueue { QueueId = 1, PlayerId = 1, ConsequenceId = 101, Status = ConsequenceStatus.pending },
            new ConsequenceQueue { QueueId = 2, PlayerId = 1, ConsequenceId = 102, Status = ConsequenceStatus.pending },
            // Even if a queue row somehow pointed at Player 2's private beat, Player 1 must not receive it
            new ConsequenceQueue { QueueId = 3, PlayerId = 1, ConsequenceId = 103, Status = ConsequenceStatus.pending },
            new ConsequenceQueue { QueueId = 4, PlayerId = 2, ConsequenceId = 101, Status = ConsequenceStatus.pending });

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.StartShift(1);

        Assert.True(result.IsSuccess);
        Assert.Equal([11, 10, 12], result.Value.Beats.Select(b => b.BeatId).ToList());
        Assert.DoesNotContain(result.Value.Beats, b => b.BeatId == 13);

        _db.ChangeTracker.Clear();
        var q1 = await _db.ConsequenceQueues.SingleAsync(q => q.QueueId == 1);
        var q2 = await _db.ConsequenceQueues.SingleAsync(q => q.QueueId == 2);
        var q3 = await _db.ConsequenceQueues.SingleAsync(q => q.QueueId == 3);
        var q4 = await _db.ConsequenceQueues.SingleAsync(q => q.QueueId == 4);

        Assert.Equal(ConsequenceStatus.fired, q1.Status);
        Assert.NotNull(q1.FiredAt);
        Assert.Equal(ConsequenceStatus.fired, q2.Status);
        Assert.NotNull(q2.FiredAt);
        Assert.Equal(ConsequenceStatus.pending, q3.Status);
        Assert.Null(q3.FiredAt);
        Assert.Equal(ConsequenceStatus.pending, q4.Status);
    }

    [Fact]
    public async Task StartShift_MidShiftResume_DoesNotFireConsequences_AndResumesFromSavedBeat()
    {
        await SeedShiftsAndPlayersAsync();

        _db.StoryBeats.AddRange(
            new StoryBeat { BeatId = 10, ShiftId = 1, BeatKey = "b1", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("1") },
            new StoryBeat { BeatId = 11, ShiftId = 1, BeatKey = "b2", BeatType = BeatType.Narrative, SequenceOrder = 2, App = BeatApp.WhatsUpp, ContentJson = Content("2") },
            new StoryBeat { BeatId = 12, ShiftId = 1, BeatKey = "b3", BeatType = BeatType.Narrative, SequenceOrder = 3, App = BeatApp.WhatsUpp, ContentJson = Content("3") },
            new StoryBeat { BeatId = 13, ShiftId = 1, BeatKey = "c1", BeatType = BeatType.Consequence, SequenceOrder = null, App = BeatApp.MailLoop, ContentJson = Content("Conseq") });
        _db.Consequences.Add(new Consequence { ConsequenceId = 101, BeatId = 13, InjectPosition = "start" });
        _db.ConsequenceQueues.Add(new ConsequenceQueue { QueueId = 1, PlayerId = 1, ConsequenceId = 101, Status = ConsequenceStatus.pending });
        _db.PlayerShiftProgresses.Add(new PlayerShiftProgress { PlayerId = 1, ShiftId = 1, Status = ShiftProgressStatus.InProgress });
        _db.PlayerSaves.Add(new PlayerSave { PlayerId = 1, BeatId = 11, SavedAt = DateTime.UtcNow.AddMinutes(-5) });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.StartShift(1);

        Assert.True(result.IsSuccess);
        Assert.Equal([11, 12], result.Value.Beats.Select(b => b.BeatId).ToList());

        var queue = await _db.ConsequenceQueues.SingleAsync(q => q.QueueId == 1);
        Assert.Equal(ConsequenceStatus.pending, queue.Status);
        Assert.Null(queue.FiredAt);
    }

    [Fact]
    public async Task StartShift_WhenShiftHasNoBeats_SucceedsWithEmptyBeats()
    {
        await SeedShiftsAndPlayersAsync();

        var result = await _sut.StartShift(1);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Beats);
        Assert.Empty(await _db.PlayerSaves.Where(s => s.PlayerId == 1).ToListAsync());
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Save_GlobalBeat_UpdatesSavedAt_AndReturnsRemainingBeats()
    {
        await SeedShiftsAndPlayersAsync();

        var oldSavedAt = DateTime.UtcNow.AddHours(-2);
        _db.StoryBeats.AddRange(
            new StoryBeat { BeatId = 10, ShiftId = 1, BeatKey = "b1", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("1") },
            new StoryBeat { BeatId = 11, ShiftId = 1, BeatKey = "b2", BeatType = BeatType.Narrative, SequenceOrder = 2, App = BeatApp.WhatsUpp, ContentJson = Content("2") },
            new StoryBeat { BeatId = 12, ShiftId = 1, BeatKey = "b3", BeatType = BeatType.Narrative, SequenceOrder = 3, App = BeatApp.WhatsUpp, ContentJson = Content("3") });
        _db.PlayerSaves.Add(new PlayerSave { PlayerId = 1, BeatId = 10, SavedAt = oldSavedAt });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.Save(1, 11);

        Assert.True(result.IsSuccess);
        Assert.Equal([11, 12], result.Value.Beats.Select(b => b.BeatId).ToList());

        var save = await _db.PlayerSaves.SingleAsync(s => s.PlayerId == 1);
        Assert.Equal(11, save.BeatId);
        Assert.True(save.SavedAt > oldSavedAt);
    }

    [Fact]
    public async Task Save_OwnPrivateAiBeat_Succeeds_AndExcludesOtherPlayerPrivateBeats()
    {
        await SeedShiftsAndPlayersAsync();

        _db.StoryBeats.AddRange(
            new StoryBeat { BeatId = 10, ShiftId = 1, BeatKey = "b1", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("1") },
            new StoryBeat { BeatId = 11, ShiftId = 1, BeatKey = "b2_p1", BeatType = BeatType.Narrative, SequenceOrder = 2, App = BeatApp.WhatsUpp, ContentJson = Content("P1 AI"), PlayerId = 1, IsAiGenerated = true },
            new StoryBeat { BeatId = 12, ShiftId = 1, BeatKey = "b3_p2", BeatType = BeatType.Narrative, SequenceOrder = 3, App = BeatApp.WhatsUpp, ContentJson = Content("P2 AI"), PlayerId = 2, IsAiGenerated = true });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.Save(1, 11);

        Assert.True(result.IsSuccess);
        Assert.Equal([11], result.Value.Beats.Select(b => b.BeatId).ToList());
        var save = await _db.PlayerSaves.SingleAsync(s => s.PlayerId == 1);
        Assert.Equal(11, save.BeatId);
    }

    [Fact]
    public async Task Save_OtherPlayerPrivateBeat_ReturnsBeatAccessForbidden()
    {
        await SeedShiftsAndPlayersAsync();

        _db.StoryBeats.Add(new StoryBeat
        {
            BeatId = 20,
            ShiftId = 1,
            BeatKey = "p2_private",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("Secret"),
            PlayerId = 2,
            IsAiGenerated = true
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.Save(1, 20);

        Assert.True(result.IsFailure);
        Assert.Equal(NarrativeErrors.BeatAccessForbidden, result.Error);
        Assert.Empty(await _db.PlayerSaves.Where(s => s.PlayerId == 1).ToListAsync());
    }

    [Fact]
    public async Task Save_WrongShiftOrNullCurrentShiftId_ReturnsFailure()
    {
        await SeedShiftsAndPlayersAsync();

        _db.StoryBeats.Add(new StoryBeat
        {
            BeatId = 30,
            ShiftId = 2,
            BeatKey = "s2_b1",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("Shift 2")
        });
        var pNullShift = new Player { PlayerId = 3, PlayerName = "NoShift", CurrentShiftId = null };
        _db.Players.Add(pNullShift);
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var wrongShift = await _sut.Save(1, 30);
        var nullShift = await _sut.Save(3, 30);

        Assert.True(wrongShift.IsFailure);
        Assert.Equal(ChoiceErrors.ShiftMismatch, wrongShift.Error);

        Assert.True(nullShift.IsFailure);
        Assert.Equal(NarrativeErrors.ShiftNotFound, nullShift.Error);
    }

    // ── EndShift ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task EndShift_InvalidOrMissingPlayerOrIncompleteGate_ReturnsFailureResult()
    {
        await SeedShiftsAndPlayersAsync();
        _db.PlayerShiftProgresses.Add(new PlayerShiftProgress { PlayerId = 1, ShiftId = 1, IsGateCleared = false });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var invalid = await _sut.EndShift(0);
        var missing = await _sut.EndShift(999);
        var incomplete = await _sut.EndShift(1);

        Assert.True(invalid.IsFailure);
        Assert.Equal(ChoiceErrors.InvalidId, invalid.Error);

        Assert.True(missing.IsFailure);
        Assert.Equal(ChoiceErrors.PlayerNotFound, missing.Error);

        Assert.True(incomplete.IsFailure);
        Assert.Equal(NarrativeErrors.ShiftNotCompleted, incomplete.Error);
    }

    [Fact]
    public async Task EndShift_WithNextShift_AdvancesShiftAndUpdatesPlayerSaveToCanonicalFirstBeat()
    {
        await SeedShiftsAndPlayersAsync();

        _db.PlayerShiftProgresses.Add(new PlayerShiftProgress { PlayerId = 1, ShiftId = 1, IsGateCleared = true });
        _db.StoryBeats.AddRange(
            new StoryBeat { BeatId = 10, ShiftId = 1, BeatKey = "s1_b1", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("s1") },
            // Shift 2 beat seq=1 is owned by another player -> EndShift must skip it and pick canonical beat seq=2
            new StoryBeat { BeatId = 20, ShiftId = 2, BeatKey = "s2_p2_ai", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("p2"), PlayerId = 2, IsAiGenerated = true },
            new StoryBeat { BeatId = 21, ShiftId = 2, BeatKey = "s2_canon", BeatType = BeatType.Narrative, SequenceOrder = 2, App = BeatApp.WhatsUpp, ContentJson = Content("s2") });
        _db.PlayerSaves.Add(new PlayerSave { PlayerId = 1, BeatId = 10 });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.EndShift(1);

        Assert.True(result.IsSuccess);
        _db.ChangeTracker.Clear();

        var updatedPlayer = await _db.Players.SingleAsync(p => p.PlayerId == 1);
        Assert.Equal(2, updatedPlayer.CurrentShiftId);

        var updatedSave = await _db.PlayerSaves.SingleAsync(s => s.PlayerId == 1);
        Assert.Equal(21, updatedSave.BeatId);
    }

    [Fact]
    public async Task EndShift_OnFinalShift_SetsCurrentShiftIdToNullWithoutNre()
    {
        var shift1 = new Shift { ShiftId = 1, ChapterNumber = 1, ShiftNumber = 1, Title = "Final", ConceptTag = Concept.Variables };
        var p1 = new Player { PlayerId = 1, PlayerName = "Finisher", CurrentShiftId = 1 };
        _db.Shifts.Add(shift1);
        _db.Players.Add(p1);
        _db.PlayerShiftProgresses.Add(new PlayerShiftProgress { PlayerId = 1, ShiftId = 1, IsGateCleared = true });
        _db.StoryBeats.Add(new StoryBeat { BeatId = 10, ShiftId = 1, BeatKey = "b1", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("end") });
        _db.PlayerSaves.Add(new PlayerSave { PlayerId = 1, BeatId = 10 });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.EndShift(1);

        Assert.True(result.IsSuccess);
        var updatedPlayer = await _db.Players.SingleAsync(p => p.PlayerId == 1);
        Assert.Null(updatedPlayer.CurrentShiftId);
    }

    // ── Admin CRUD Isolation ─────────────────────────────────────────────────

    [Fact]
    public async Task GetShift_And_UpdateShift_OmitPrivateAndAiGeneratedBeats()
    {
        await SeedShiftsAndPlayersAsync();

        _db.StoryBeats.AddRange(
            new StoryBeat { BeatId = 10, ShiftId = 1, BeatKey = "canon_nav", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("Canon nav") },
            new StoryBeat { BeatId = 11, ShiftId = 1, BeatKey = "ai_nav", BeatType = BeatType.Narrative, SequenceOrder = 2, App = BeatApp.WhatsUpp, ContentJson = Content("AI nav"), PlayerId = 1, IsAiGenerated = true },
            new StoryBeat { BeatId = 12, ShiftId = 1, BeatKey = "canon_conseq", BeatType = BeatType.Consequence, SequenceOrder = null, App = BeatApp.MailLoop, ContentJson = Content("Canon conseq") },
            new StoryBeat { BeatId = 13, ShiftId = 1, BeatKey = "ai_conseq", BeatType = BeatType.Consequence, SequenceOrder = null, App = BeatApp.MailLoop, ContentJson = Content("AI conseq"), PlayerId = 1, IsAiGenerated = true });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var getResult = await _sut.GetShift(1);
        Assert.True(getResult.IsSuccess);
        Assert.Equal([10], getResult.Value.NarrativeBeats.Select(b => b.BeatId).ToList());
        Assert.Equal([12], getResult.Value.ConsequenceBeats.Select(b => b.BeatId).ToList());

        _db.ChangeTracker.Clear();
        var updateResult = await _sut.UpdateShift(1, new UpdateShiftDto { Title = "Renamed Shift" });
        Assert.True(updateResult.IsSuccess);
        Assert.Equal("Renamed Shift", updateResult.Value.Title);
        Assert.Equal([10], updateResult.Value.NarrativeBeats.Select(b => b.BeatId).ToList());
        Assert.Equal([12], updateResult.Value.ConsequenceBeats.Select(b => b.BeatId).ToList());
    }

    [Fact]
    public async Task AdminBeatCrud_RejectsDynamicOrPlayerOwnedBeats_WithDynamicBeatForbidden()
    {
        await SeedShiftsAndPlayersAsync();

        _db.StoryBeats.Add(new StoryBeat
        {
            BeatId = 50,
            ShiftId = 1,
            BeatKey = "ai_owned",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("AI"),
            PlayerId = 1,
            IsAiGenerated = true
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var getBeat = await _sut.GetStoryBeat(50);
        var updateBeat = await _sut.UpdateStoryBeat(50, new UpdateStoryBeatDto { DelaySeconds = 3m });
        var deleteBeat = await _sut.DeleteStoryBeat(50);
        var assignBeat = await _sut.AssignBeatToShift(50, 2);

        Assert.True(getBeat.IsFailure);
        Assert.Equal(NarrativeErrors.DynamicBeatForbidden, getBeat.Error);

        Assert.True(updateBeat.IsFailure);
        Assert.Equal(NarrativeErrors.DynamicBeatForbidden, updateBeat.Error);

        Assert.True(deleteBeat.IsFailure);
        Assert.Equal(NarrativeErrors.DynamicBeatForbidden, deleteBeat.Error);

        Assert.True(assignBeat.IsFailure);
        Assert.Equal(NarrativeErrors.DynamicBeatForbidden, assignBeat.Error);

        var stillExists = await _db.StoryBeats.SingleAsync(b => b.BeatId == 50);
        Assert.Equal(1, stillExists.ShiftId);
    }

    [Fact]
    public async Task CanonicalSequenceConflictChecks_IgnorePrivateAiBeats_WhileBlockingCanonicalCollisions()
    {
        await SeedShiftsAndPlayersAsync();

        _db.StoryBeats.AddRange(
            new StoryBeat { BeatId = 60, ShiftId = 1, BeatKey = "ai_s1_seq1", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("AI 1"), PlayerId = 1, IsAiGenerated = true },
            new StoryBeat { BeatId = 61, ShiftId = 1, BeatKey = "ai_s1_seq2", BeatType = BeatType.Narrative, SequenceOrder = 2, App = BeatApp.WhatsUpp, ContentJson = Content("AI 2"), PlayerId = 1, IsAiGenerated = true },
            new StoryBeat { BeatId = 62, ShiftId = 2, BeatKey = "ai_s2_seq2", BeatType = BeatType.Narrative, SequenceOrder = 2, App = BeatApp.WhatsUpp, ContentJson = Content("AI s2"), PlayerId = 1, IsAiGenerated = true });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // 1. Create canonical beat at seq=1 on Shift 1 despite AI beat at seq=1
        var created = await _sut.CreateStoryBeat(new CreateStoryBeatDto
        {
            ShiftId = 1,
            BeatKey = "canon_s1_seq1",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("Canon 1")
        });
        Assert.True(created.IsSuccess);
        _db.ChangeTracker.Clear();

        // 2. Canonical collision at seq=1 on Shift 1 MUST still fail
        var collision = await _sut.CreateStoryBeat(new CreateStoryBeatDto
        {
            ShiftId = 1,
            BeatKey = "canon_s1_seq1_dup",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("Dup")
        });
        Assert.True(collision.IsFailure);
        Assert.Equal(NarrativeErrors.SequenceOrderConflict, collision.Error);
        _db.ChangeTracker.Clear();

        // 3. Update canonical beat to seq=2 on Shift 1 despite AI beat at seq=2
        var updated = await _sut.UpdateStoryBeat(created.Value.BeatId, new UpdateStoryBeatDto { SequenceOrder = 2 });
        Assert.True(updated.IsSuccess);
        Assert.Equal(2, updated.Value.SequenceOrder);
        _db.ChangeTracker.Clear();

        // 4. Assign canonical beat (seq=2) to Shift 2 despite AI beat at seq=2 on Shift 2
        var assigned = await _sut.AssignBeatToShift(created.Value.BeatId, 2);
        Assert.True(assigned.IsSuccess);
    }

    public void Dispose() => _db.Dispose();
}
