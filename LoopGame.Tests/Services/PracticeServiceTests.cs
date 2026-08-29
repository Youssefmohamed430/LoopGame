using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Application.Services.LearningAndContentServices;
using LoopGame.Domain.Entities.Code;

namespace LoopGame.Tests.Services;

/// <summary>
/// Unit tests for PracticeService covering GetTaskAsync, AddPracticeTask,
/// and UpdatePracticeTask with authorization and null-safety guards.
/// </summary>
public class PracticeServiceTests : IDisposable
{
    private const int PlayerId = 1;
    private const int TaskId = 10;
    private const int ShiftId = 100;

    private readonly AppDbContext _db;
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICodeExecutionService> _codeExecutionService = new();
    private readonly PracticeService _sut;

    public PracticeServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);

        _uow.Setup(u => u.GetRepository<PracticeTask>())
            .Returns(new BaseRepository<PracticeTask>(_db));
        _uow.Setup(u => u.GetRepository<Player>())
            .Returns(new BaseRepository<Player>(_db));
        _uow.Setup(u => u.GetRepository<TestCase>())
            .Returns(new BaseRepository<TestCase>(_db));
        _uow.Setup(u => u.GetRepository<Shift>())
            .Returns(new BaseRepository<Shift>(_db));

        _uow.Setup(u => u.SaveAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken ct) => _db.SaveChangesAsync(ct));

        _sut = new PracticeService(_uow.Object, _codeExecutionService.Object);
    }

    [Fact]
    public void GetTaskAsync_TaskNotFound_ReturnsFailureResult()
    {
        // Act
        var result = _sut.GetTaskAsync(TaskId, PlayerId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Task", result.Error.Code);
    }

    [Fact]
    public void GetTaskAsync_PlayerNotFound_ReturnsFailureResult()
    {
        // Arrange
        _db.PracticeTasks.Add(new PracticeTask
        {
            TaskId = TaskId,
            ShiftId = ShiftId,
            Title = "Binary Search",
            Description = "Implement Binary Search algorithm."
        });
        _db.SaveChanges();

        // Act
        var result = _sut.GetTaskAsync(TaskId, PlayerId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Player", result.Error.Code);
    }

    [Fact]
    public void GetTaskAsync_PlayerHasNoActiveShift_ReturnsFailureResult()
    {
        // Arrange
        _db.PracticeTasks.Add(new PracticeTask
        {
            TaskId = TaskId,
            ShiftId = ShiftId,
            Title = "Binary Search"
        });
        _db.Players.Add(new Player
        {
            PlayerId = PlayerId,
            CurrentShiftId = null,
            CurrentShift = null
        });
        _db.SaveChanges();

        // Act
        var result = _sut.GetTaskAsync(TaskId, PlayerId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden.Access", result.Error.Code);
    }

    [Fact]
    public void GetTaskAsync_TaskNotAssignedToPlayerShift_ReturnsForbiddenFailureResult()
    {
        // Arrange
        var otherShift = new Shift { ShiftId = 200, ShiftNumber = 2, Title = "Other Shift" };
        var currentShift = new Shift { ShiftId = ShiftId, ShiftNumber = 1, Title = "Current Shift" };

        _db.Shifts.AddRange(currentShift, otherShift);
        _db.PracticeTasks.Add(new PracticeTask
        {
            TaskId = TaskId,
            ShiftId = 200, // task belongs to another shift
            Title = "Advanced Algorithms"
        });
        _db.Players.Add(new Player
        {
            PlayerId = PlayerId,
            CurrentShiftId = ShiftId,
            CurrentShift = currentShift
        });
        _db.SaveChanges();

        // Act
        var result = _sut.GetTaskAsync(TaskId, PlayerId);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden.Access", result.Error.Code);
    }

    [Fact]
    public void GetTaskAsync_ValidPlayerAndShiftTask_ReturnsTaskWithHiddenTestCasesFilteredOut()
    {
        // Arrange
        var shift = new Shift { ShiftId = ShiftId, ShiftNumber = 1, Title = "Shift 1" };
        var task = new PracticeTask
        {
            TaskId = TaskId,
            ShiftId = ShiftId,
            Title = "Array Sum",
            Shift = shift,
            TestCases = new List<TestCase>
            {
                new TestCase { TestCaseId = 1, TaskId = TaskId, TestInput = "1 2", ExpectedOutput = "3", IsHidden = false },
                new TestCase { TestCaseId = 2, TaskId = TaskId, TestInput = "5 5", ExpectedOutput = "10", IsHidden = true }
            }
        };

        shift.PracticeTasks.Add(task);

        _db.Shifts.Add(shift);
        _db.PracticeTasks.Add(task);
        _db.Players.Add(new Player
        {
            PlayerId = PlayerId,
            CurrentShiftId = ShiftId,
            CurrentShift = shift
        });
        _db.SaveChanges();

        // Act
        var result = _sut.GetTaskAsync(TaskId, PlayerId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(TaskId, result.Value.TaskId);
        Assert.NotNull(result.Value.TestCases);
        Assert.Single(result.Value.TestCases);
        Assert.False(result.Value.TestCases[0].IsHidden);
        Assert.Equal("1 2", result.Value.TestCases[0].TestInput);
    }

    [Fact]
    public void AddPracticeTask_ValidTask_AddsTaskToDatabase()
    {
        // Arrange
        var dto = new PracticeDto
        {
            TaskId = 15,
            Title = "New Task",
            Description = "Task description",
            TestCases = new List<TestCaseDto>
            {
                new TestCaseDto { TestCaseId = 100, TestInput = "input", ExpectedOutput = "output", IsHidden = false }
            }
        };

        // Act
        var result = _sut.AddPracticeTask(dto);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("New Task", result.Value.Title);
    }

    [Fact]
    public void UpdatePracticeTask_TaskNotFound_ReturnsFailureResult()
    {
        // Arrange
        var dto = new PracticeDto { Title = "Updated Title" };

        // Act
        var result = _sut.UpdatePracticeTask(999, dto);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound.Task", result.Error.Code);
    }

    [Fact]
    public void UpdatePracticeTask_ExistingTask_UpdatesTaskProperties()
    {
        // Arrange
        var shift = new Shift { ShiftId = ShiftId, Title = "Test Shift" };
        var task = new PracticeTask
        {
            TaskId = TaskId,
            ShiftId = ShiftId,
            Shift = shift,
            Title = "Old Title",
            Description = "Old Description",
            Difficulty = "Standard"
        };
        _db.Shifts.Add(shift);
        _db.PracticeTasks.Add(task);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();

        var updateDto = new PracticeDto
        {
            Title = "New Updated Title",
            Difficulty = "Challenge"
        };

        // Act
        var result = _sut.UpdatePracticeTask(TaskId, updateDto);

        // Assert
        Assert.True(result.IsSuccess);
        var updatedTask = _db.PracticeTasks.First(t => t.TaskId == TaskId);
        Assert.Equal("New Updated Title", updatedTask.Title);
        Assert.Equal("Challenge", updatedTask.Difficulty);
        Assert.Equal("Old Description", updatedTask.Description);
    }

    public void Dispose() => _db.Dispose();
}
