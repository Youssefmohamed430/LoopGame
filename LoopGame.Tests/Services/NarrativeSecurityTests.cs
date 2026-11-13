using System.Security.Claims;
using LoopGame.Application.Dtos.NarrativeDtos;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Application.Services.EconomyAndProgressionServices;
using LoopGame.Controllers;
using LoopGame.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Tests.Services;

public class NarrativeSecurityTests : IDisposable
{
    private readonly Mock<INarrativeService> _narrativeMock = new();
    private readonly Mock<IChoiceService> _choiceMock = new();

    private readonly AppDbContext _db;
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly EconomyService _economySut;

    public NarrativeSecurityTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _uow.Setup(u => u.GetRepository<Transaction>()).Returns(new BaseRepository<Transaction>(_db));
        _uow.Setup(u => u.GetRepository<PlayerEconomy>()).Returns(new BaseRepository<PlayerEconomy>(_db));
        _uow.Setup(u => u.GetRepository<Player>()).Returns(new BaseRepository<Player>(_db));
        _uow.Setup(u => u.GetRepository<PlayerChoice>()).Returns(new BaseRepository<PlayerChoice>(_db));

        _uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => _db.SaveChangesAsync(ct));
        _uow.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _economySut = new EconomyService(_uow.Object, new FakeEconomyRepository(_db));
    }

    private sealed class FakeEconomyRepository(AppDbContext db) : IPlayerEconomyRepository
    {
        public bool HasActiveTransaction => false;

        public Task<PlayerEconomy?> GetForUpdateAsync(int playerId, CancellationToken ct = default)
            => db.PlayerEconomies.FirstOrDefaultAsync(p => p.PlayerId == playerId, ct);
    }

    private static ControllerContext CreateUserContext(string userId, string? role = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
        if (!string.IsNullOrEmpty(role))
            claims.Add(new(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private static ControllerContext CreateUnauthenticatedContext() => new()
    {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
    };

    // ── Controller IDOR & Authentication Checks ──────────────────────────────

    [Fact]
    public async Task AllEndpoints_WhenUnauthenticated_ReturnForbid_AndNeverCallServices()
    {
        var controller = new NarrativeController(_narrativeMock.Object, _choiceMock.Object)
        {
            ControllerContext = CreateUnauthenticatedContext()
        };

        Assert.IsType<ForbidResult>((await controller.StartShift(42)).Result);
        Assert.IsType<ForbidResult>((await controller.Save(42, 10)).Result);
        Assert.IsType<ForbidResult>((await controller.EndShift(42)).Result);
        Assert.IsType<ForbidResult>((await controller.GetChoices(42, 10)).Result);
        Assert.IsType<ForbidResult>((await controller.SubmitChoice(42, 5)).Result);

        _narrativeMock.Verify(n => n.StartShift(It.IsAny<int>()), Times.Never);
        _narrativeMock.Verify(n => n.Save(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _narrativeMock.Verify(n => n.EndShift(It.IsAny<int>()), Times.Never);
        _choiceMock.Verify(c => c.GetChoices(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _choiceMock.Verify(c => c.SubmitChoice(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AllEndpoints_WhenAuthenticatedAsDifferentPlayer_ReturnForbid_AndNeverCallServices()
    {
        var controller = new NarrativeController(_narrativeMock.Object, _choiceMock.Object)
        {
            ControllerContext = CreateUserContext("10")
        };

        Assert.IsType<ForbidResult>((await controller.StartShift(42)).Result);
        Assert.IsType<ForbidResult>((await controller.Save(42, 10)).Result);
        Assert.IsType<ForbidResult>((await controller.EndShift(42)).Result);
        Assert.IsType<ForbidResult>((await controller.GetChoices(42, 10)).Result);
        Assert.IsType<ForbidResult>((await controller.SubmitChoice(42, 5)).Result);

        _narrativeMock.Verify(n => n.StartShift(It.IsAny<int>()), Times.Never);
        _narrativeMock.Verify(n => n.Save(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _narrativeMock.Verify(n => n.EndShift(It.IsAny<int>()), Times.Never);
        _choiceMock.Verify(c => c.GetChoices(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        _choiceMock.Verify(c => c.SubmitChoice(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AllEndpoints_WhenAuthenticatedAsOwner_PassThroughAndReturnOk()
    {
        var flow = new NarrativeFlowDto { ShiftId = 1, Shift = new ShiftDto { ShiftId = 1, Title = "S1" }, Beats = [] };
        List<ChoiceDto> choices = [new ChoiceDto { ChoiceId = 5, BeatId = 10, ChoiceIndex = 1, ChoiceText = "Go" }];
        var submitted = choices[0];

        _narrativeMock.Setup(n => n.StartShift(42)).ReturnsAsync(Result.Success(flow));
        _narrativeMock.Setup(n => n.Save(42, 10)).ReturnsAsync(Result.Success(flow));
        _narrativeMock.Setup(n => n.EndShift(42)).ReturnsAsync(Result.Success<object>(new { msg = "End Shift Success!" }));
        _choiceMock.Setup(c => c.GetChoices(10, 42)).ReturnsAsync(Result.Success(choices));
        _choiceMock.Setup(c => c.SubmitChoice(5, 42)).ReturnsAsync(Result.Success(submitted));

        var controller = new NarrativeController(_narrativeMock.Object, _choiceMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        Assert.IsType<OkObjectResult>((await controller.StartShift(42)).Result);
        Assert.IsType<OkObjectResult>((await controller.Save(42, 10)).Result);
        Assert.IsType<OkObjectResult>((await controller.EndShift(42)).Result);
        Assert.IsType<OkObjectResult>((await controller.GetChoices(42, 10)).Result);
        Assert.IsType<OkObjectResult>((await controller.SubmitChoice(42, 5)).Result);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("admin")]
    [InlineData("super_admin")]
    public async Task StartShift_WhenAdminRole_AllowsCrossPlayerAccess(string role)
    {
        var flow = new NarrativeFlowDto { ShiftId = 1, Shift = new ShiftDto { ShiftId = 1, Title = "S1" }, Beats = [] };
        _narrativeMock.Setup(n => n.StartShift(42)).ReturnsAsync(Result.Success(flow));

        var controller = new NarrativeController(_narrativeMock.Object, _choiceMock.Object)
        {
            ControllerContext = CreateUserContext("1", role: role)
        };

        var result = await controller.StartShift(42);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task ControllerErrorMapping_MapsDomainFailuresToExpectedHttpStatusCodes()
    {
        _narrativeMock.Setup(n => n.EndShift(42)).ReturnsAsync(Result.Failure<object>(NarrativeErrors.ShiftNotCompleted));
        _narrativeMock.Setup(n => n.Save(42, 99)).ReturnsAsync(Result.Failure<NarrativeFlowDto>(NarrativeErrors.BeatAccessForbidden));
        _choiceMock.Setup(c => c.SubmitChoice(5, 42)).ReturnsAsync(Result.Failure<ChoiceDto>(ChoiceErrors.DuplicateChoiceSubmission));

        var controller = new NarrativeController(_narrativeMock.Object, _choiceMock.Object)
        {
            ControllerContext = CreateUserContext("42")
        };

        var endRes = Assert.IsType<ObjectResult>((await controller.EndShift(42)).Result);
        Assert.Equal(400, endRes.StatusCode);
        Assert.Equal(NarrativeErrors.ShiftNotCompleted.Code, Assert.IsType<ApiErrorResponse>(endRes.Value).Code);

        var saveRes = Assert.IsType<ObjectResult>((await controller.Save(42, 99)).Result);
        Assert.Equal(403, saveRes.StatusCode);

        var submitRes = Assert.IsType<ObjectResult>((await controller.SubmitChoice(42, 5)).Result);
        Assert.Equal(409, submitRes.StatusCode);
    }

    // ── EconomyService Invariant 2: AI Beat Choices Excluded from Salary ─────

    [Fact]
    public async Task PayShiftSalary_IgnoresChoicesOnAiGeneratedBeats()
    {
        const int playerId = 1;
        const int shiftId = 1;

        _db.Players.Add(new Player { PlayerId = playerId, PlayerName = "InternPlayer", Rank = PlayerRank.Intern }); // base 2000m
        _db.PlayerEconomies.Add(new PlayerEconomy(playerId));
        _db.Shifts.Add(new Shift { ShiftId = shiftId, ChapterNumber = 1, ShiftNumber = 1, Title = "Shift 1" });
        _db.StoryBeats.AddRange(
            new StoryBeat
            {
                BeatId = 10,
                ShiftId = shiftId,
                BeatKey = "canon_beat",
                ContentJson = new StoryBeatContent("Canon", null, null, null),
                PlayerId = null,
                IsAiGenerated = false
            },
            new StoryBeat
            {
                BeatId = 20,
                ShiftId = shiftId,
                BeatKey = "ai_beat_p1",
                ContentJson = new StoryBeatContent("AI", null, null, null),
                PlayerId = playerId,
                IsAiGenerated = true
            });

        // 1 canonical Debt choice (0% bonus) + 3 AI Ideal choices (must be ignored!)
        _db.PlayerChoices.AddRange(
            new PlayerChoice { PlayerId = playerId, BeatId = 10, ChoiceId = 1, Tier = ChoiceTier.Debt },
            new PlayerChoice { PlayerId = playerId, BeatId = 20, ChoiceId = 2, Tier = ChoiceTier.Ideal },
            new PlayerChoice { PlayerId = playerId, BeatId = 20, ChoiceId = 3, Tier = ChoiceTier.Ideal },
            new PlayerChoice { PlayerId = playerId, BeatId = 20, ChoiceId = 4, Tier = ChoiceTier.Ideal });

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _economySut.PayShiftSalaryAsync(playerId, shiftId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2000m, result.Value); // strictly base salary, zero bonus from AI choices
    }

    [Fact]
    public async Task PayShiftSalary_ComputesBonusStrictlyFromCanonicalBeats_WhenMixedWithAiChoices()
    {
        const int playerId = 1;
        const int shiftId = 1;

        _db.Players.Add(new Player { PlayerId = playerId, PlayerName = "InternPlayer", Rank = PlayerRank.Intern }); // base 2000m
        _db.PlayerEconomies.Add(new PlayerEconomy(playerId));
        _db.Shifts.Add(new Shift { ShiftId = shiftId, ChapterNumber = 1, ShiftNumber = 1, Title = "Shift 1" });
        _db.StoryBeats.AddRange(
            new StoryBeat
            {
                BeatId = 10,
                ShiftId = shiftId,
                BeatKey = "canon_1",
                ContentJson = new StoryBeatContent("Canon 1", null, null, null),
                IsAiGenerated = false
            },
            new StoryBeat
            {
                BeatId = 11,
                ShiftId = shiftId,
                BeatKey = "canon_2",
                ContentJson = new StoryBeatContent("Canon 2", null, null, null),
                IsAiGenerated = false
            },
            new StoryBeat
            {
                BeatId = 20,
                ShiftId = shiftId,
                BeatKey = "ai_1",
                ContentJson = new StoryBeatContent("AI", null, null, null),
                PlayerId = playerId,
                IsAiGenerated = true
            });

        // Canonical: 1 Ideal + 1 Debt = 50% Ideal -> bonus rate 0.10 -> 200m bonus -> 2200m total.
        // AI: 4 Ideal choices that must NOT inflate bonus above 200m.
        _db.PlayerChoices.AddRange(
            new PlayerChoice { PlayerId = playerId, BeatId = 10, ChoiceId = 1, Tier = ChoiceTier.Ideal },
            new PlayerChoice { PlayerId = playerId, BeatId = 11, ChoiceId = 2, Tier = ChoiceTier.Debt },
            new PlayerChoice { PlayerId = playerId, BeatId = 20, ChoiceId = 3, Tier = ChoiceTier.Ideal },
            new PlayerChoice { PlayerId = playerId, BeatId = 20, ChoiceId = 4, Tier = ChoiceTier.Ideal },
            new PlayerChoice { PlayerId = playerId, BeatId = 20, ChoiceId = 5, Tier = ChoiceTier.Ideal },
            new PlayerChoice { PlayerId = playerId, BeatId = 20, ChoiceId = 6, Tier = ChoiceTier.Ideal });

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _economySut.PayShiftSalaryAsync(playerId, shiftId);

        Assert.True(result.IsSuccess);
        Assert.Equal(2200m, result.Value);
    }

    public void Dispose() => _db.Dispose();
}
