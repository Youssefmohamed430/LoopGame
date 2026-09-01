using TestCaseResult = LoopGame.Domain.ValueObjects.TestCaseResult;
using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Application.Services.LearningAndContentServices;
using LoopGame.Domain.Constants;
using LoopGame.Domain.Entities.Assessment;
using LoopGame.Domain.Entities.Code;
using LoopGame.Domain.Entities.Narrative;
using LoopGame.Domain.Entities.Player;
using LoopGame.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LoopGame.Tests.Services;

public class AssessmentServiceTests : IDisposable
{
    private const int PlayerId = 1;
    private const int ShiftId = 10;
    private const string ConceptTag = "loops";

    private readonly AppDbContext _db;
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly AssessmentService _assessmentService;

    public AssessmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _uow.Setup(u => u.GetRepository<Player>())
            .Returns(new BaseRepository<Player>(_db));
        _uow.Setup(u => u.GetRepository<Shift>())
            .Returns(new BaseRepository<Shift>(_db));
        _uow.Setup(u => u.GetRepository<AssessmentEvent>())
            .Returns(new BaseRepository<AssessmentEvent>(_db));
        _uow.Setup(u => u.GetRepository<ConceptMasterySnapshot>())
            .Returns(new BaseRepository<ConceptMasterySnapshot>(_db));

        _uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => _db.SaveChangesAsync(ct));

        _assessmentService = new AssessmentService(_uow.Object, NullLogger<AssessmentService>.Instance);
    }

    private async Task SeedPlayerAndShiftAsync()
    {
        var player = new Player
        {
            PlayerId = PlayerId,
            StudentIdHash = "hash123",
            CurrentShiftId = ShiftId
        };
        var shift = new Shift
        {
            ShiftId = ShiftId,
            ShiftNumber = 1,
            Title = "Introduction to Loops"
        };

        _db.Players.Add(player);
        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Test1_ComputeMastery_PracticeIdeal_And_GateCleared_UsesOnlyPracticeEvidence()
    {
        await SeedPlayerAndShiftAsync();

        // PracticeAttempt = Ideal (weight 2.5)
        _db.AssessmentEvents.Add(new AssessmentEvent
        {
            PlayerId = PlayerId,
            EventType = AssessmentWeights.EventTypes.PracticeAttempt,
            ConceptTag = ConceptTag,
            Tier = nameof(ChoiceTier.Ideal),
            RecordedAt = DateTime.UtcNow
        });

        // GateCleared (progression event - weight 0.0)
        _db.AssessmentEvents.Add(new AssessmentEvent
        {
            PlayerId = PlayerId,
            EventType = AssessmentWeights.EventTypes.GateCleared,
            ConceptTag = ConceptTag, // Historical tag test
            Tier = null,
            RecordedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var result = await _assessmentService.ComputeMasteryAsync(PlayerId, ShiftId);

        Assert.True(result.IsSuccess);

        var snapshot = await _db.ConceptMasterySnapshots
            .FirstOrDefaultAsync(s => s.PlayerId == PlayerId && s.ConceptTag == ConceptTag);

        Assert.NotNull(snapshot);
        // Only 1 genuine evidence item (PracticeAttempt) should be counted
        Assert.Equal(1, snapshot.EvidenceCount);

        // Expected score: weightedSum = 2.5 * 1.0 = 2.5. decayDenominator = 1.0. rawScore = 2.5.
        // Sigmoid(2.5) with midpoint 5.0 = 1 / (1 + e^2.5) ≈ 0.07585
        double expectedSigmoid = 1.0 / (1.0 + Math.Exp(-1.0 * (2.5 - 5.0)));
        Assert.Equal((decimal)expectedSigmoid, snapshot.MasteryScore, 4);
    }

    [Fact]
    public async Task Test2_ComputeMastery_PracticeAcceptable_And_GateCleared_DoesNotDoubleCount()
    {
        await SeedPlayerAndShiftAsync();

        // PracticeAttempt = Acceptable (weight 2.0)
        _db.AssessmentEvents.Add(new AssessmentEvent
        {
            PlayerId = PlayerId,
            EventType = AssessmentWeights.EventTypes.PracticeAttempt,
            ConceptTag = ConceptTag,
            Tier = nameof(ChoiceTier.Acceptable),
            RecordedAt = DateTime.UtcNow
        });

        // GateCleared (progression event)
        _db.AssessmentEvents.Add(new AssessmentEvent
        {
            PlayerId = PlayerId,
            EventType = AssessmentWeights.EventTypes.GateCleared,
            ConceptTag = ConceptTag,
            Tier = null,
            RecordedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var result = await _assessmentService.ComputeMasteryAsync(PlayerId, ShiftId);

        Assert.True(result.IsSuccess);

        var snapshot = await _db.ConceptMasterySnapshots
            .FirstOrDefaultAsync(s => s.PlayerId == PlayerId && s.ConceptTag == ConceptTag);

        Assert.NotNull(snapshot);
        Assert.Equal(1, snapshot.EvidenceCount); // GateCleared excluded from evidence count

        double expectedSigmoid = 1.0 / (1.0 + Math.Exp(-1.0 * (2.0 - 5.0)));
        Assert.Equal((decimal)expectedSigmoid, snapshot.MasteryScore, 4);
    }

    [Fact]
    public async Task Test3_ComputeMastery_GateClearedOnly_DoesNotCreateArtificialPositiveMastery()
    {
        await SeedPlayerAndShiftAsync();

        // Only GateCleared with null ConceptTag
        _db.AssessmentEvents.Add(new AssessmentEvent
        {
            PlayerId = PlayerId,
            EventType = AssessmentWeights.EventTypes.GateCleared,
            ConceptTag = null,
            Tier = null,
            RecordedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        var result = await _assessmentService.ComputeMasteryAsync(PlayerId, ShiftId);

        Assert.True(result.IsSuccess);

        // No concept snapshots should be created for non-concept-specific GateCleared
        var count = await _db.ConceptMasterySnapshots.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Test4_ProgressionFlow_IdealAttempt_ClearsGate()
    {
        // Setup emitter spy
        var emittedEvents = new List<AssessmentEventDto>();
        var emitterMock = new Mock<IAssessmentEventEmitter>();
        emitterMock.Setup(e => e.Emit(It.IsAny<AssessmentEventDto>()))
            .Callback<AssessmentEventDto>(e => emittedEvents.Add(e));

        var schedulerMock = new Mock<IAssessmentJobScheduler>();

        var shiftProgress = new PlayerShiftProgress
        {
            PlayerId = PlayerId,
            ShiftId = ShiftId,
            Status = ShiftProgressStatus.InProgress
        };

        var task = new PracticeTask
        {
            TaskId = 1,
            ShiftId = ShiftId,
            ConceptTag = ConceptTag,
            MaxAttempts = 3,
            TestCases = [new TestCase { TestCaseId = 1, TestInput = "in", ExpectedOutput = "out" }]
        };

        var shift = new Shift
        {
            ShiftId = ShiftId,
            ShiftNumber = 1,
            Title = "Shift 1",
            PracticeTasks = [task]
        };

        var player = new Player
        {
            PlayerId = PlayerId,
            StudentIdHash = "hash123",
            CurrentShiftId = ShiftId,
            CurrentShift = shift,
            ShiftProgresses = [shiftProgress]
        };

        var executionMock = new Mock<ICodeExecutionService>();
        executionMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<List<TestCase>>()))
            .ReturnsAsync([new TestCaseResult(1, true, "out", 10)]);

        _uow.Setup(u => u.GetRepository<Player>())
            .Returns(new BaseRepository<Player>(_db));
        _uow.Setup(u => u.GetRepository<PracticeTask>())
            .Returns(new BaseRepository<PracticeTask>(_db));
        _uow.Setup(u => u.GetRepository<PracticeAttempt>())
            .Returns(new BaseRepository<PracticeAttempt>(_db));
        _uow.Setup(u => u.GetRepository<PlayerShiftProgress>())
            .Returns(new BaseRepository<PlayerShiftProgress>(_db));

        _db.Shifts.Add(shift);
        _db.Players.Add(player);
        _db.PracticeTasks.Add(task);
        await _db.SaveChangesAsync();

        var practiceService = new PracticeService(
            _uow.Object,
            new PracticeAccessService(_uow.Object),
            new MaxAttemptsPolicy(_uow.Object),
            executionMock.Object,
            new PracticeTierCalculationPolicy(),
            new PracticeAttemptService(_uow.Object),
            new ProgressionService(_uow.Object),
            emitterMock.Object,
            schedulerMock.Object);

        var submitResult = await practiceService.SubmitCode(PlayerId, new CodeSubmitRequestDto
        {
            TaskId = 1,
            SubmittedCode = "return true;",
            TimeSpentSec = 10,
            HintUsed = false
        });

        Assert.True(submitResult.IsSuccess);
        Assert.True(submitResult.Value.GateCleared);
        Assert.True(shiftProgress.IsGateCleared);
        Assert.Equal(ShiftProgressStatus.Completed, shiftProgress.Status);
    }

    [Fact]
    public async Task Test5_GateCleared_EmittedWithNullConceptTag()
    {
        // Setup emitter spy
        var emittedEvents = new List<AssessmentEventDto>();
        var emitterMock = new Mock<IAssessmentEventEmitter>();
        emitterMock.Setup(e => e.Emit(It.IsAny<AssessmentEventDto>()))
            .Callback<AssessmentEventDto>(e => emittedEvents.Add(e));

        var schedulerMock = new Mock<IAssessmentJobScheduler>();

        var task = new PracticeTask
        {
            TaskId = 1,
            ShiftId = ShiftId,
            ConceptTag = ConceptTag,
            MaxAttempts = 3,
            TestCases = [new TestCase { TestCaseId = 1, TestInput = "in", ExpectedOutput = "out" }]
        };

        var shift = new Shift
        {
            ShiftId = ShiftId,
            ShiftNumber = 1,
            Title = "Shift 1",
            PracticeTasks = [task]
        };

        var player = new Player
        {
            PlayerId = PlayerId,
            StudentIdHash = "hash123",
            CurrentShiftId = ShiftId,
            CurrentShift = shift,
            ShiftProgresses = [
                new PlayerShiftProgress { PlayerId = PlayerId, ShiftId = ShiftId, Status = ShiftProgressStatus.InProgress }
            ]
        };

        var executionMock = new Mock<ICodeExecutionService>();
        executionMock.Setup(x => x.ExecuteAsync(It.IsAny<string>(), It.IsAny<List<TestCase>>()))
            .ReturnsAsync([new TestCaseResult(1, true, "out", 10)]);

        _uow.Setup(u => u.GetRepository<Player>())
            .Returns(new BaseRepository<Player>(_db));
        _uow.Setup(u => u.GetRepository<PracticeTask>())
            .Returns(new BaseRepository<PracticeTask>(_db));
        _uow.Setup(u => u.GetRepository<PracticeAttempt>())
            .Returns(new BaseRepository<PracticeAttempt>(_db));
        _uow.Setup(u => u.GetRepository<PlayerShiftProgress>())
            .Returns(new BaseRepository<PlayerShiftProgress>(_db));

        _db.Shifts.Add(shift);
        _db.Players.Add(player);
        _db.PracticeTasks.Add(task);
        await _db.SaveChangesAsync();

        var practiceService = new PracticeService(
            _uow.Object,
            new PracticeAccessService(_uow.Object),
            new MaxAttemptsPolicy(_uow.Object),
            executionMock.Object,
            new PracticeTierCalculationPolicy(),
            new PracticeAttemptService(_uow.Object),
            new ProgressionService(_uow.Object),
            emitterMock.Object,
            schedulerMock.Object);

        var submitResult = await practiceService.SubmitCode(PlayerId, new CodeSubmitRequestDto
        {
            TaskId = 1,
            SubmittedCode = "return true;",
            TimeSpentSec = 10,
            HintUsed = false
        });

        Assert.True(submitResult.IsSuccess);

        var gateClearedEvent = emittedEvents.FirstOrDefault(e => e.EventType == AssessmentWeights.EventTypes.GateCleared);
        Assert.NotNull(gateClearedEvent);
        Assert.Null(gateClearedEvent.ConceptTag); // GateCleared MUST have ConceptTag = null
    }

    public void Dispose() => _db.Dispose();
}
