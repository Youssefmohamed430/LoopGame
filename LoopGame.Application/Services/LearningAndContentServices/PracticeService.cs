using LoopGame.Domain.Entities.Player;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;

namespace LoopGame.Application.Services.LearningAndContentServices;

public class PracticeService
    (IUnitOfWork unitOfWork, ICodeExecutionService codeExecutionService)
    : IPracticeService
{
    public async Task<Result<PracticeDto>> GetTaskAsync(int TaskId, int PlayerId)
    {
        var task = unitOfWork.GetRepository<PracticeTask>()
            .Find<PracticeDto>(pt => pt.TaskId == TaskId, new string[] { "TestCases", "Shift" });

        if (task is null)
        {
            return Result.Failure<PracticeDto>(new Error("NotFound.Task", $"Task with ID '{TaskId}' was not found."));
        }
        
        var (player, failure) = await CheckAccess(PlayerId, TaskId);
        if(failure != null) return (Result<PracticeDto>)failure;

        task.TestCases = task.TestCases?.Where(t => !t.IsHidden).ToList();

        return Result.Success(task);
    }

    public async Task<Result<CodeSubmitResponseDto>> SubmitCode(int PlayerId, CodeSubmitRequestDto code)
    {
        // - Check Player Access
        var (player, failure) = await CheckAccess(PlayerId, code.TaskId);
        if(failure != null) return (Result<CodeSubmitResponseDto>)failure;

        // 1. Get PracticeTask & Get TestCases
        if (GetTasksWithHiddenTests(code, out var task, out var result1)) return result1;
        
        // - Check For MaxAttempts Validation
        if (ValidateMaxAttempts(PlayerId, code.TaskId, task.MaxAttempts, out var submitCode1)) return submitCode1;

        // 2. Execute Code
        var result = await codeExecutionService.ExecuteAsync(code.SubmittedCode, task.TestCases.ToList());

        // 3. Claculate Tier
        var tier = CalculateTier(result.ToList());

        // 4. INSERT PracticeAttempt
        await InsertPracticeAttempts(PlayerId, code, tier, result);

        // 5.gate_attempts++ 
        var (playerProgress, failure1) = await UpdatePlayerProgress(player);
        if(failure1 != null) return (Result<CodeSubmitResponseDto>)failure1;
        

        // 6. Check Gate Status
        UpdateGateStatus(code, player, playerProgress);

        await unitOfWork?.SaveAsync()!;

        return Result.Success<CodeSubmitResponseDto>(
        new CodeSubmitResponseDto
        {
            Tier = tier,
            TestResults = JsonSerializer.Serialize(result),
            GateCleared = playerProgress.Status == ShiftProgressStatus.Completed,
            StruggleDetected = player.PracticeAttempts.Where(p => p.TaskId == code.TaskId).Count() > 4,
         });
    }

    private bool GetTasksWithHiddenTests(CodeSubmitRequestDto code, out PracticeTask? task, out Result<CodeSubmitResponseDto> result1)
    {
        task = unitOfWork.GetRepository<PracticeTask>()
            .Find(t => t.TaskId == code.TaskId,new string[] { "TestCases", "Shift" });


        if (task is null)
        {
            result1 = Result.Failure<CodeSubmitResponseDto>(
                new Error(
                    "NotFound.Task",
                    $"Task with ID '{code.TaskId}' was not found."));
            return true;
        }

        result1 = null;
        return false;
    }

    private static void UpdateGateStatus(CodeSubmitRequestDto code, Player player, PlayerShiftProgress? playerProgress)
    {
        var attempts = player.PracticeAttempts
            .Take(3)
            .Where(p => (p.TaskId == code.TaskId && (p.Tier == ChoiceTier.Ideal || p.Tier == ChoiceTier.Acceptable)))
            .OrderByDescending(p => p.SubmittedAt);

        if (attempts.Count() != 3)
            playerProgress.Status = ShiftProgressStatus.Completed;
        else if (attempts.Count() < 3 && attempts.Count() > 0)
            playerProgress.Status = ShiftProgressStatus.GatePending;
    }

    private async Task<(PlayerShiftProgress? playerProgress, Result<CodeSubmitResponseDto> failure1)> UpdatePlayerProgress(Player player)
    {
        var playerProgress = player.ShiftProgresses.FirstOrDefault(p => p.ShiftId == player.CurrentShiftId);

        if (playerProgress is null)
        {
            return (playerProgress, Result.Failure<CodeSubmitResponseDto>(
                new Error(
                    "NotFound.Progress",
                    "Player shift progress was not found.")));
        }
        playerProgress.GateAttempts++;
        await unitOfWork?.GetRepository<PlayerShiftProgress>()?.UpdateAsync(playerProgress)!;
        return (playerProgress, null!);
    }

    private async Task InsertPracticeAttempts(int PlayerId, CodeSubmitRequestDto code, ChoiceTier tier, List<TestCaseResult> result)
    {
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
    }

    private bool ValidateMaxAttempts(int PlayerId, int TaskId , int MaxAttempts, out Result<CodeSubmitResponseDto> submitCode1)
    {
        var attemptsCount = unitOfWork
            .GetRepository<PracticeAttempt>()
            .FindAll(a =>
                a.PlayerId == PlayerId &&
                a.TaskId == TaskId).Count();

        if (MaxAttempts > 0 &&
            attemptsCount >= MaxAttempts)
        {
            submitCode1 = Result.Failure<CodeSubmitResponseDto>(
                new Error(
                    "Practice.MaxAttemptsReached",
                    "Maximum attempts reached for this task."));
            return true;
        }

        submitCode1 = null;
        return false;
    }

    private async Task<(Player player, Result failure)> CheckAccess(int PlayerId, int TaskId)
    {
        var player = await unitOfWork.GetRepository<Player>()
            .FindAsync(p => p.PlayerId == PlayerId, new string[] { "PracticeAttempts", "ShiftProgresses" , "CurrentShift.PracticeTasks"});

        if (player is null)
            return (null!, Result.Failure(new Error("Forbidden.AccessGame", "You are not allowed to access this game.")));
        
        if (player.CurrentShift is null)
            return (null!,Result.Failure(new Error("Forbidden.Access", "Player has no active shift.")));
        
        if (!player.CurrentShift.PracticeTasks.Any(t => t.TaskId == TaskId))
            return (null!, Result.Failure(new Error("Forbidden.Access", "You are not allowed to access this task.")));

        return (player, null!);
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
