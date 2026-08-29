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

    public CodeSubmitResponseDto SubmitCode(int PlayerId, CodeSubmitRequestDto code)
    {
        throw new NotImplementedException();
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
