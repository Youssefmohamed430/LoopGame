

using System.Threading.Tasks;

namespace LoopGame.Application.Services.LearningAndContentServices;

public class PracticeService
    (IUnitOfWork unitOfWork,ICodeExecutionService codeExecutionService)
    : IPracticeService
{
    public Result<PracticeDto> GetTaskAsync(int TaskId, int PlayerId)
    {
        var task = unitOfWork.GetRepository<PracticeTask>()
            .Find<PracticeDto>(pt => pt.TaskId == TaskId, new string[] { "TestCases", "Shift", "Attempts" });

        return Result<PracticeDto>.Success(task);
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
        AddTestCasesAtPracticeTask(practice?.TestCases);
        unitOfWork.SaveAsync();
        return Result<PracticeDto>.Success(practice);
    }

    //public Result<PracticeDto> DeletePracticeTask(int TaskId)
    //{
    //    var task = unitOfWork.GetRepository<PracticeTask>()
    //        .Find<PracticeDto>(pt => pt.TaskId == TaskId, new string[] { "TestCases", "Shift", "Attempts" });

    //    foreach (var test in task?.TestCases)
    //    {
    //        unitOfWork.GetRepository<TestCase>()
    //            .DeleteAsync(test.Adapt<TestCase>());
    //    }
    //}

    public Result<PracticeDto> UpdatePracticeTask(int TaskId, PracticeDto practice)
    {
        var task = unitOfWork.GetRepository<PracticeTask>()
            .Find(pt => pt.TaskId == TaskId, new string[] { "TestCases", "Shift", "Attempts" });
        HandleUpdate(practice, task);
        unitOfWork.GetRepository<PracticeTask>().UpdateAsync(task);
        unitOfWork.SaveAsync();
        return Result<PracticeDto>.Success(practice);
    }

    private static void HandleUpdate(PracticeDto practice, PracticeTask task)
    {
        task.MaxAttempts = practice.MaxAttempts ?? task.MaxAttempts;
        task.StarterCode = practice.StarterCode ?? task.StarterCode;
        task.ConceptTag = practice.ConceptTag ?? task.ConceptTag;
        task.Description = practice.Description ?? task.Description;
        task.Difficulty = practice.Difficulty ?? task.Difficulty;
        task.TaskOrder = practice.TaskOrder ?? task.TaskOrder;
        task.Title = practice.Title ?? task.Title;
        task.EgpReward = practice.EgpReward ?? task.EgpReward;
    }

    public Result<TestCaseDto> UpdateTestCasesAtPracticeTask(int TestId,TestCaseDto testCaseDto)
    {
        var testcase = unitOfWork.GetRepository<TestCase>()
            .Find(tc => tc.TestCaseId == TestId);

        testcase.TestInput = testCaseDto.TestInput ?? testcase.TestInput;
        testcase.Description = testCaseDto.Description ?? testcase.Description;
        testcase.ExpectedOutput = testCaseDto.ExpectedOutput ?? testcase.ExpectedOutput;
        testcase.IsHidden = testCaseDto.IsHidden;

        unitOfWork.GetRepository<TestCase>().UpdateAsync(testcase);
        unitOfWork.SaveAsync();
        return Result<TestCaseDto>.Success(testCaseDto);
    }

    public Result<List<TestCaseDto>> AddTestCasesAtPracticeTask(List<TestCaseDto> testCaseDtos)
    {
        foreach (TestCaseDto testCase in testCaseDtos)
        {
            var test = testCase.Adapt<TestCase>();
            unitOfWork.GetRepository<TestCase>()
                .AddAsync(test);
        }
        unitOfWork.SaveAsync();
        return Result<List<TestCaseDto>>.Success(testCaseDtos);
    }

    
}
