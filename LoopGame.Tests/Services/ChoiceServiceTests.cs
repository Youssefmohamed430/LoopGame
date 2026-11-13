using LoopGame.Application.Services.LearningAndContentServices;

namespace LoopGame.Tests.Services;

public class ChoiceServiceTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly List<GameEventDto> _emittedEvents = [];
    private readonly ChoiceService _sut;

    public ChoiceServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _uow.Setup(u => u.GetRepository<Player>()).Returns(new BaseRepository<Player>(_db));
        _uow.Setup(u => u.GetRepository<StoryBeat>()).Returns(new BaseRepository<StoryBeat>(_db));
        _uow.Setup(u => u.GetRepository<Choice>()).Returns(new BaseRepository<Choice>(_db));
        _uow.Setup(u => u.GetRepository<Consequence>()).Returns(new BaseRepository<Consequence>(_db));
        _uow.Setup(u => u.GetRepository<PlayerChoice>()).Returns(new BaseRepository<PlayerChoice>(_db));
        _uow.Setup(u => u.GetRepository<PlayerShiftProgress>()).Returns(new BaseRepository<PlayerShiftProgress>(_db));
        _uow.Setup(u => u.GetRepository<ConsequenceQueue>()).Returns(new BaseRepository<ConsequenceQueue>(_db));

        _uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => _db.SaveChangesAsync(ct));

        var publisherMock = new Mock<IEventPublisher>();
        publisherMock.Setup(p => p.Publish(It.IsAny<GameEventDto>()))
            .Callback<GameEventDto>(e => _emittedEvents.Add(e));

        _sut = new ChoiceService(_uow.Object, publisherMock.Object);
    }

    private static StoryBeatContent Content(string text) => new(text, null, null, null);

    private async Task SeedBaseWorldAsync()
    {
        _db.Shifts.AddRange(
            new Shift { ShiftId = 1, ChapterNumber = 1, ShiftNumber = 1, Title = "Shift 1", ConceptTag = Concept.Loops },
            new Shift { ShiftId = 2, ChapterNumber = 1, ShiftNumber = 2, Title = "Shift 2", ConceptTag = Concept.Arrays });

        _db.Players.AddRange(
            new Player { PlayerId = 1, PlayerName = "P1", CurrentShiftId = 1 },
            new Player { PlayerId = 2, PlayerName = "P2", CurrentShiftId = 1 },
            new Player { PlayerId = 3, PlayerName = "NoShift", CurrentShiftId = null });

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
    }

    // ── GetChoices ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetChoices_GlobalBeat_ReturnsChoicesOrderedByIndex()
    {
        await SeedBaseWorldAsync();

        _db.StoryBeats.Add(new StoryBeat
        {
            BeatId = 10,
            ShiftId = 1,
            BeatKey = "global_beat",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("Choose"),
            HasChoices = true,
            PlayerId = null,
            IsAiGenerated = false
        });
        _db.Choices.AddRange(
            new Choice { ChoiceId = 102, BeatId = 10, ChoiceIndex = 2, ChoiceText = "Second", Tier = ChoiceTier.Acceptable },
            new Choice { ChoiceId = 101, BeatId = 10, ChoiceIndex = 1, ChoiceText = "First", Tier = ChoiceTier.Ideal });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.GetChoices(10, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal([101, 102], result.Value.Select(c => c.ChoiceId).ToList());
    }

    [Fact]
    public async Task GetChoices_OwnPrivateBeat_Succeeds_AndOtherPlayerPrivateBeat_ReturnsForbidden()
    {
        await SeedBaseWorldAsync();

        _db.StoryBeats.Add(new StoryBeat
        {
            BeatId = 20,
            ShiftId = 1,
            BeatKey = "p1_ai",
            BeatType = BeatType.Narrative,
            SequenceOrder = 2,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("AI prompt"),
            HasChoices = true,
            PlayerId = 1,
            IsAiGenerated = true
        });
        _db.Choices.Add(new Choice { ChoiceId = 201, BeatId = 20, ChoiceIndex = 1, ChoiceText = "Reply", Tier = ChoiceTier.Ideal });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var ownerResult = await _sut.GetChoices(20, 1);
        var otherResult = await _sut.GetChoices(20, 2);

        Assert.True(ownerResult.IsSuccess);
        Assert.Single(ownerResult.Value);

        Assert.True(otherResult.IsFailure);
        Assert.Equal(ChoiceErrors.ForbiddenAccess, otherResult.Error);
    }

    [Fact]
    public async Task GetChoices_MissingPlayerMissingBeatOrWrongShift_ReturnsExpectedErrors()
    {
        await SeedBaseWorldAsync();

        _db.StoryBeats.Add(new StoryBeat
        {
            BeatId = 30,
            ShiftId = 2,
            BeatKey = "s2_beat",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("Shift 2")
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        Assert.Equal(ChoiceErrors.InvalidId, (await _sut.GetChoices(0, 1)).Error);
        Assert.Equal(ChoiceErrors.PlayerNotFound, (await _sut.GetChoices(30, 999)).Error);
        Assert.Equal(ChoiceErrors.BeatNotFound, (await _sut.GetChoices(999, 1)).Error);
        Assert.Equal(ChoiceErrors.ShiftMismatch, (await _sut.GetChoices(30, 1)).Error);
        Assert.Equal(ChoiceErrors.ShiftMismatch, (await _sut.GetChoices(30, 3)).Error);
    }

    // ── SubmitChoice ─────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitChoice_MissingEntitiesOrNullShiftOrMissingProgress_FailsSafelyWithoutNre()
    {
        await SeedBaseWorldAsync();

        _db.StoryBeats.Add(new StoryBeat
        {
            BeatId = 10,
            ShiftId = 1,
            BeatKey = "canon_eval",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("Eval beat"),
            HasChoices = true
        });
        _db.Choices.Add(new Choice
        {
            ChoiceId = 101,
            BeatId = 10,
            ChoiceIndex = 1,
            ChoiceText = "Ideal",
            Tier = ChoiceTier.Ideal,
            IsEvaluateable = true
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var badIds = await _sut.SubmitChoice(0, 1);
        var noPlayer = await _sut.SubmitChoice(101, 999);
        var nullShift = await _sut.SubmitChoice(101, 3);
        var noChoice = await _sut.SubmitChoice(999, 1);
        // Player 1 has no PlayerShiftProgress for Shift 1 yet
        var noProgress = await _sut.SubmitChoice(101, 1);

        Assert.Equal(ChoiceErrors.InvalidId, badIds.Error);
        Assert.Equal(ChoiceErrors.PlayerNotFound, noPlayer.Error);
        Assert.Equal(ChoiceErrors.ShiftMismatch, nullShift.Error);
        Assert.Equal(ChoiceErrors.ChoiceNotFound, noChoice.Error);
        Assert.Equal(ChoiceErrors.ShiftProgressNotFound, noProgress.Error);
        Assert.Empty(await _db.PlayerChoices.ToListAsync());
        Assert.Empty(_emittedEvents);
    }

    [Fact]
    public async Task SubmitChoice_OtherPlayerPrivateBeat_OrPrivateConsequenceBeat_ReturnsForbiddenAccess()
    {
        await SeedBaseWorldAsync();

        _db.StoryBeats.AddRange(
            new StoryBeat { BeatId = 20, ShiftId = 1, BeatKey = "p2_ai", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("P2"), HasChoices = true, PlayerId = 2, IsAiGenerated = true },
            new StoryBeat { BeatId = 21, ShiftId = 1, BeatKey = "canon_b", BeatType = BeatType.Narrative, SequenceOrder = 2, App = BeatApp.WhatsUpp, ContentJson = Content("Canon"), HasChoices = true },
            new StoryBeat { BeatId = 22, ShiftId = 2, BeatKey = "p2_conseq", BeatType = BeatType.Consequence, SequenceOrder = null, App = BeatApp.MailLoop, ContentJson = Content("P2 conseq"), PlayerId = 2, IsAiGenerated = true });
        _db.Consequences.Add(new Consequence { ConsequenceId = 50, BeatId = 22, InjectPosition = "start" });
        _db.Choices.AddRange(
            new Choice { ChoiceId = 201, BeatId = 20, ChoiceIndex = 1, ChoiceText = "Other AI choice", Tier = ChoiceTier.Ideal, IsEvaluateable = false },
            new Choice { ChoiceId = 202, BeatId = 21, ChoiceIndex = 1, ChoiceText = "Links P2 consequence", Tier = ChoiceTier.Ideal, IsEvaluateable = false, ConsequenceId = 50 });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var r1 = await _sut.SubmitChoice(201, 1);
        var r2 = await _sut.SubmitChoice(202, 1);

        Assert.True(r1.IsFailure);
        Assert.Equal(ChoiceErrors.ForbiddenAccess, r1.Error);

        Assert.True(r2.IsFailure);
        Assert.Equal(ChoiceErrors.ForbiddenAccess, r2.Error);

        Assert.Empty(await _db.PlayerChoices.ToListAsync());
        Assert.Empty(await _db.ConsequenceQueues.ToListAsync());
    }

    [Fact]
    public async Task SubmitChoice_DuplicateForSameBeat_ReturnsDuplicateChoiceSubmission()
    {
        await SeedBaseWorldAsync();

        _db.PlayerShiftProgresses.Add(new PlayerShiftProgress { PlayerId = 1, ShiftId = 1, GateAttempts = 0 });
        _db.StoryBeats.Add(new StoryBeat
        {
            BeatId = 10,
            ShiftId = 1,
            BeatKey = "canon",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("Choose"),
            HasChoices = true
        });
        _db.Choices.AddRange(
            new Choice { ChoiceId = 101, BeatId = 10, ChoiceIndex = 1, ChoiceText = "A", Tier = ChoiceTier.Ideal, IsEvaluateable = true },
            new Choice { ChoiceId = 102, BeatId = 10, ChoiceIndex = 2, ChoiceText = "B", Tier = ChoiceTier.Debt, IsEvaluateable = true });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var first = await _sut.SubmitChoice(101, 1);
        _db.ChangeTracker.Clear();
        var second = await _sut.SubmitChoice(102, 1);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal(ChoiceErrors.DuplicateChoiceSubmission, second.Error);

        Assert.Single(await _db.PlayerChoices.Where(pc => pc.PlayerId == 1 && pc.BeatId == 10).ToListAsync());
        Assert.Equal((short)1, (await _db.PlayerShiftProgresses.SingleAsync(p => p.PlayerId == 1 && p.ShiftId == 1)).GateAttempts);
        Assert.Single(_emittedEvents);
    }

    [Fact]
    public async Task SubmitChoice_NonEvaluatableChoiceWithConsequence_PersistsPlayerChoiceAndConsequenceQueue()
    {
        await SeedBaseWorldAsync();

        _db.PlayerShiftProgresses.Add(new PlayerShiftProgress { PlayerId = 1, ShiftId = 1, GateAttempts = 0 });
        _db.StoryBeats.AddRange(
            new StoryBeat { BeatId = 10, ShiftId = 1, BeatKey = "story_flavor", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("Flavor"), HasChoices = true },
            new StoryBeat { BeatId = 11, ShiftId = 2, BeatKey = "deferred_conseq", BeatType = BeatType.Consequence, SequenceOrder = null, App = BeatApp.MailLoop, ContentJson = Content("Fallout") });
        _db.Consequences.Add(new Consequence { ConsequenceId = 77, BeatId = 11, InjectPosition = "start" });
        _db.Choices.Add(new Choice
        {
            ChoiceId = 101,
            BeatId = 10,
            ChoiceIndex = 1,
            ChoiceText = "Flavor option",
            Tier = ChoiceTier.Acceptable,
            IsEvaluateable = false,
            ConsequenceId = 77
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.SubmitChoice(101, 1);

        Assert.True(result.IsSuccess);

        var pc = await _db.PlayerChoices.SingleOrDefaultAsync(x => x.PlayerId == 1 && x.ChoiceId == 101);
        Assert.NotNull(pc);
        Assert.Equal(10, pc.BeatId);

        var queued = await _db.ConsequenceQueues.SingleOrDefaultAsync(q => q.PlayerId == 1 && q.ConsequenceId == 77);
        Assert.NotNull(queued);
        Assert.Equal(ConsequenceStatus.pending, queued.Status);

        var progress = await _db.PlayerShiftProgresses.SingleAsync(p => p.PlayerId == 1 && p.ShiftId == 1);
        Assert.Equal((short)0, progress.GateAttempts);
        Assert.Empty(_emittedEvents);
    }

    [Fact]
    public async Task SubmitChoice_AiGeneratedBeat_EnforcesInvariant2_NeverIncrementsGateAttemptsOrPublishesTelemetry()
    {
        await SeedBaseWorldAsync();

        _db.PlayerShiftProgresses.Add(new PlayerShiftProgress { PlayerId = 1, ShiftId = 1, GateAttempts = 2 });
        _db.StoryBeats.AddRange(
            new StoryBeat
            {
                BeatId = 40,
                ShiftId = 1,
                BeatKey = "ai_branch_p1",
                BeatType = BeatType.Narrative,
                SequenceOrder = 2,
                App = BeatApp.WhatsUpp,
                ContentJson = Content("AI branch"),
                HasChoices = true,
                PlayerId = 1,
                IsAiGenerated = true
            },
            new StoryBeat
            {
                BeatId = 41,
                ShiftId = 2,
                BeatKey = "ai_conseq_p1",
                BeatType = BeatType.Consequence,
                SequenceOrder = null,
                App = BeatApp.MailLoop,
                ContentJson = Content("AI consequence"),
                PlayerId = 1,
                IsAiGenerated = true
            });
        _db.Consequences.Add(new Consequence { ConsequenceId = 88, BeatId = 41, InjectPosition = "start" });
        // Even if IsEvaluateable were accidentally true on an AI beat choice, Invariant 2 blocks assessment side-effects
        _db.Choices.Add(new Choice
        {
            ChoiceId = 401,
            BeatId = 40,
            ChoiceIndex = 1,
            ChoiceText = "AI Reply",
            Tier = ChoiceTier.Ideal,
            IsEvaluateable = true,
            ConsequenceId = 88
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.SubmitChoice(401, 1);

        Assert.True(result.IsSuccess);

        var pc = await _db.PlayerChoices.SingleOrDefaultAsync(x => x.PlayerId == 1 && x.ChoiceId == 401);
        Assert.NotNull(pc);

        var queued = await _db.ConsequenceQueues.SingleOrDefaultAsync(q => q.PlayerId == 1 && q.ConsequenceId == 88);
        Assert.NotNull(queued);

        var progress = await _db.PlayerShiftProgresses.SingleAsync(p => p.PlayerId == 1 && p.ShiftId == 1);
        Assert.Equal((short)2, progress.GateAttempts);
        Assert.Empty(_emittedEvents);
    }

    [Fact]
    public async Task SubmitChoice_EvaluatableCanonicalChoice_IncrementsGateAttemptsAndPublishesEvent()
    {
        await SeedBaseWorldAsync();

        _db.PlayerShiftProgresses.Add(new PlayerShiftProgress { PlayerId = 1, ShiftId = 1, GateAttempts = 0 });
        _db.StoryBeats.Add(new StoryBeat
        {
            BeatId = 10,
            ShiftId = 1,
            BeatKey = "canon_eval",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            ContentJson = Content("Eval"),
            HasChoices = true,
            PlayerId = null,
            IsAiGenerated = false
        });
        _db.Choices.Add(new Choice
        {
            ChoiceId = 101,
            BeatId = 10,
            ChoiceIndex = 1,
            ChoiceText = "Best answer",
            Tier = ChoiceTier.Ideal,
            IsEvaluateable = true
        });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.SubmitChoice(101, 1);

        Assert.True(result.IsSuccess);
        var progress = await _db.PlayerShiftProgresses.SingleAsync(p => p.PlayerId == 1 && p.ShiftId == 1);
        Assert.Equal((short)1, progress.GateAttempts);

        var emitted = Assert.Single(_emittedEvents);
        Assert.Equal(1, emitted.PlayerId);
        Assert.Equal(AssessmentWeights.EventTypes.ChoiceSubmission, emitted.EventType);
        Assert.Equal(Concept.Loops, emitted.ConceptTag);
        Assert.Equal(nameof(ChoiceTier.Ideal), emitted.Tier);
    }

    // ── Admin CRUD (AddChoice / UpdateChoice) ────────────────────────────────

    [Fact]
    public async Task AddChoice_OnDynamicBeat_OrWithDynamicConsequence_ReturnsDynamicBeatForbidden()
    {
        await SeedBaseWorldAsync();

        _db.StoryBeats.AddRange(
            new StoryBeat { BeatId = 10, ShiftId = 1, BeatKey = "canon", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("Canon"), HasChoices = true },
            new StoryBeat { BeatId = 20, ShiftId = 1, BeatKey = "ai", BeatType = BeatType.Narrative, SequenceOrder = 2, App = BeatApp.WhatsUpp, ContentJson = Content("AI"), HasChoices = true, PlayerId = 1, IsAiGenerated = true },
            new StoryBeat { BeatId = 21, ShiftId = 2, BeatKey = "ai_c", BeatType = BeatType.Consequence, SequenceOrder = null, App = BeatApp.MailLoop, ContentJson = Content("AI conseq"), PlayerId = 1, IsAiGenerated = true });
        _db.Consequences.Add(new Consequence { ConsequenceId = 60, BeatId = 21, InjectPosition = "start" });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var onAiBeat = await _sut.AddChoice([new CreateChoiceDto { BeatId = 20, ChoiceIndex = 1, ChoiceText = "Hack", Tier = ChoiceTier.Ideal }]);
        var withAiConseq = await _sut.AddChoice([new CreateChoiceDto { BeatId = 10, ChoiceIndex = 1, ChoiceText = "Cross", Tier = ChoiceTier.Ideal, ConsequenceId = 60 }]);

        Assert.True(onAiBeat.IsFailure);
        Assert.Equal(ChoiceErrors.DynamicBeatForbidden, onAiBeat.Error);

        Assert.True(withAiConseq.IsFailure);
        Assert.Equal(ChoiceErrors.DynamicBeatForbidden, withAiConseq.Error);

        Assert.Empty(await _db.Choices.ToListAsync());
    }

    [Fact]
    public async Task UpdateChoice_OnDynamicBeat_OrLinkingDynamicConsequence_ReturnsDynamicBeatForbidden()
    {
        await SeedBaseWorldAsync();

        _db.StoryBeats.AddRange(
            new StoryBeat { BeatId = 10, ShiftId = 1, BeatKey = "canon", BeatType = BeatType.Narrative, SequenceOrder = 1, App = BeatApp.WhatsUpp, ContentJson = Content("Canon"), HasChoices = true },
            new StoryBeat { BeatId = 20, ShiftId = 1, BeatKey = "ai", BeatType = BeatType.Narrative, SequenceOrder = 2, App = BeatApp.WhatsUpp, ContentJson = Content("AI"), HasChoices = true, PlayerId = 1, IsAiGenerated = true },
            new StoryBeat { BeatId = 21, ShiftId = 2, BeatKey = "ai_c", BeatType = BeatType.Consequence, SequenceOrder = null, App = BeatApp.MailLoop, ContentJson = Content("AI conseq"), PlayerId = 1, IsAiGenerated = true });
        _db.Consequences.Add(new Consequence { ConsequenceId = 60, BeatId = 21, InjectPosition = "start" });
        _db.Choices.AddRange(
            new Choice { ChoiceId = 101, BeatId = 10, ChoiceIndex = 1, ChoiceText = "Canon choice", Tier = ChoiceTier.Ideal },
            new Choice { ChoiceId = 201, BeatId = 20, ChoiceIndex = 1, ChoiceText = "AI choice", Tier = ChoiceTier.Ideal });
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var editAiChoice = await _sut.UpdateChoice(201, new UpdateChoiceDto { ChoiceText = "Mutated" });
        var linkAiConseq = await _sut.UpdateChoice(101, new UpdateChoiceDto { ConsequenceId = 60 });

        Assert.True(editAiChoice.IsFailure);
        Assert.Equal(ChoiceErrors.DynamicBeatForbidden, editAiChoice.Error);

        Assert.True(linkAiConseq.IsFailure);
        Assert.Equal(ChoiceErrors.DynamicBeatForbidden, linkAiConseq.Error);
    }

    public void Dispose() => _db.Dispose();
}
