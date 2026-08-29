using LoopGame.Domain.Entities.Player;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

namespace LoopGame.Application.Services.LearningAndContentServices;

public class PracticeService
    (IUnitOfWork unitOfWork, ICodeExecutionService codeExecutionService)
    : IPracticeService
{
    public Result<PracticeDto> GetTaskAsync(int TaskId, int PlayerId)
    {
        var task = unitOfWork.GetRepository<PracticeTask>()
            .Find<PracticeDto>(pt => pt.TaskId == TaskId, new string[] { "TestCases", "Shift" });

        if (task is null)
        {
            return Result.Failure<PracticeDto>(new Error("NotFound.Task", $"Task with ID '{TaskId}' was not found."));
        }

        var player = unitOfWork.GetRepository<Player>()
            .Find(p => p.PlayerId == PlayerId, new string[] { "CurrentShift.PracticeTasks" });

        if (player is null)
        {
            return Result.Failure<PracticeDto>(new Error("NotFound.Player", $"Player with ID '{PlayerId}' was not found."));
        }

        if (player.CurrentShift is null)
        {
            return Result.Failure<PracticeDto>(new Error("Forbidden.Access", "Player has no active shift."));
        }

        if (!player.CurrentShift.PracticeTasks.Any(t => t.TaskId == TaskId))
        {
            return Result.Failure<PracticeDto>(new Error("Forbidden.Access", "You are not allowed to access this task."));
        }

        task.TestCases = task.TestCases?.Where(t => !t.IsHidden).ToList();

        return Result.Success(task);
    }

    public async Task<Result<CodeSubmitResponseDto>> SubmitCode(int PlayerId, CodeSubmitRequestDto code)
    {
        // - Check Player Access

        var player = await unitOfWork.GetRepository<Player>()
            .FindAsync(p => p.PlayerId == PlayerId, new string[] { "PracticeAttempts", "ShiftProgresses" });

        if (!player.CurrentShift.PracticeTasks.Any(t => t.TaskId == code.TaskId))
        {
            return Result.Failure<CodeSubmitResponseDto>(new Error("Forbidden.Access", "You are not allowed to access this task."));
        }

        // 1. Get PracticeTask & Get TestCases
        var task = unitOfWork.GetRepository<PracticeTask>()
            .Find(t => t.TaskId == code.TaskId,new string[] { "TestCases", "Shift" });


        if (task is null)
        {
            return Result.Failure<CodeSubmitResponseDto>(
                new Error(
                    "NotFound.Task",
                    $"Task with ID '{code.TaskId}' was not found."));
        }


        // - Check For MaxAttempts Validation

        var attemptsCount = unitOfWork
            .GetRepository<PracticeAttempt>()
            .FindAll(a =>
        a.PlayerId == PlayerId &&
        a.TaskId == code.TaskId).Count();

        if (task.MaxAttempts > 0 &&
            attemptsCount >= task.MaxAttempts)
        {
            return Result.Failure<CodeSubmitResponseDto>(
                new Error(
                    "Practice.MaxAttemptsReached",
                    "Maximum attempts reached for this task."));
        }

        // 2. Execute Code
        var result = await codeExecutionService.ExecuteAsync(code.SubmittedCode, task.TestCases.ToList());

        // 3. Claculate Tier
        var tier = CalculateTier(result.ToList());

        // 4. INSERT PracticeAttempt
        var practiceAttemptes = new PracticeAttempt()
        {
            PlayerId = PlayerId,
            TaskId = code.TaskId, 
            SubmittedCode = code.SubmittedCode,
            Tier = tier,
            TestResults = JsonSerializer.Serialize(result),
            HintUsed = code.HintUsed,
            TimeSpentSec = code.TimeSpentSec
        };
        await unitOfWork.GetRepository<PracticeAttempt>()
            .AddAsync(practiceAttemptes);

        // 5.gate_attempts++ 
        var playerProgress = player.ShiftProgresses.FirstOrDefault(p => p.ShiftId == player.CurrentShiftId);

        if (playerProgress is null)
        {
            return Result.Failure<CodeSubmitResponseDto>(
                new Error(
                    "NotFound.Progress",
                    "Player shift progress was not found."));
        }
        playerProgress.GateAttempts++;
        await unitOfWork?.GetRepository<PlayerShiftProgress>()?.UpdateAsync(playerProgress)!;
        await unitOfWork?.SaveAsync()!;

        // 6. Check Gate Status
        var attempts = player.PracticeAttempts
            .Take(3)
            .Where(p => (p.TaskId == code.TaskId && (p.Tier == ChoiceTier.Ideal || p.Tier == ChoiceTier.Acceptable)))
            .OrderByDescending(p => p.SubmittedAt);

        if (attempts.Count() != 3)
            playerProgress.Status = ShiftProgressStatus.Completed;
        else if (attempts.Count() < 3 && attempts.Count() > 0)
            playerProgress.Status = ShiftProgressStatus.GatePending;


        return Result.Success<CodeSubmitResponseDto>(
        new CodeSubmitResponseDto
        {
            Tier = tier,
            TestResults = JsonSerializer.Serialize(result),
            GateCleared = playerProgress.Status == ShiftProgressStatus.Completed,
            StruggleDetected = player.PracticeAttempts.Where(p => p.TaskId == code.TaskId).Count() > 4,
         });
    }
    private ChoiceTier CalculateTier(List<TestCaseResult> results)
    {
        if (results.Count == 0)
            return ChoiceTier.Mistake;

        var passed = results.Count(x => x.Passed);

        if (passed == results.Count)
        {
            // TODO:
            // Code quality analysis For Acceptable
            return ChoiceTier.Ideal;
        }

        if (passed > 0)
            return ChoiceTier.Debt;

        return ChoiceTier.Mistake;
    }

    public Result<PracticeDto> AddPracticeTask(PracticeDto practice)
    {
        var task = practice.Adapt<PracticeTask>();
        unitOfWork.GetRepository<PracticeTask>()
            .AddAsync(task);
        unitOfWork.SaveAsync().GetAwaiter().GetResult();
        return Result.Success(practice);
    }
    public Result<PracticeDto> UpdatePracticeTask(int TaskId, PracticeDto practice)
    {
        var task = unitOfWork.GetRepository<PracticeTask>()
            .FindWithTracking(pt => pt.TaskId == TaskId, new string[] { "TestCases", "Shift", "Attempts" });
        if (task is null)
            return Result.Failure<PracticeDto>(new Error("NotFound.Task", "Task is NotFound."));
        HandleUpdate(practice, task);
        unitOfWork.GetRepository<PracticeTask>().UpdateAsync(task);
        unitOfWork.SaveAsync().GetAwaiter().GetResult();
        return Result.Success(practice);
    }

    private static void HandleUpdate(PracticeDto practice, PracticeTask task)
    {
        task.MaxAttempts = practice.MaxAttempts ?? task.MaxAttempts;
        task.StarterCode = practice.StarterCode ?? task.StarterCode;
        task.ConceptTag = !string.IsNullOrWhiteSpace(practice.ConceptTag) ? practice.ConceptTag : task.ConceptTag;
        task.Description = !string.IsNullOrWhiteSpace(practice.Description) ? practice.Description : task.Description;
        task.Difficulty = !string.IsNullOrWhiteSpace(practice.Difficulty) ? practice.Difficulty : task.Difficulty;
        task.TaskOrder = practice.TaskOrder ?? task.TaskOrder;
        task.Title = !string.IsNullOrWhiteSpace(practice.Title) ? practice.Title : task.Title;
        task.EgpReward = practice.EgpReward ?? task.EgpReward;
    }

    public Result<TestCaseDto> UpdateTestCasesAtPracticeTask(int TestId, TestCaseDto testCaseDto)
    {
        var testcase = unitOfWork.GetRepository<TestCase>()
            .FindWithTracking(tc => tc.TestCaseId == TestId);
        if (testcase is null)
            return Result.Failure<TestCaseDto>(new Error("NotFound.TestCase", "TestCase was not found."));

        testcase.TestInput = testCaseDto.TestInput ?? testcase.TestInput;
        testcase.Description = testCaseDto.Description ?? testcase.Description;
        testcase.ExpectedOutput = testCaseDto.ExpectedOutput ?? testcase.ExpectedOutput;
        testcase.IsHidden = testCaseDto.IsHidden;

        unitOfWork.GetRepository<TestCase>().UpdateAsync(testcase);
        unitOfWork.SaveAsync().GetAwaiter().GetResult();
        return Result.Success(testCaseDto);
    }

    public Result<List<TestCaseDto>> AddTestCasesAtPracticeTask(List<TestCaseDto> testCaseDtos)
    {
        foreach (TestCaseDto testCase in testCaseDtos)
        {
            var test = testCase.Adapt<TestCase>();
            unitOfWork.GetRepository<TestCase>()
                .AddAsync(test);
        }
        unitOfWork.SaveAsync().GetAwaiter().GetResult();
        return Result.Success(testCaseDtos);
    }

    
}
