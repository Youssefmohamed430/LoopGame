namespace LoopGame.Application.IServices.LearningAndContentServices;

public interface IPracticeService
{
    Task<Result<PracticeDto>> GetTaskAsync(int TaskId,int PlayerId);
    Result<PracticeDto> AddPracticeTask(PracticeDto practice);
    Result<PracticeDto> UpdatePracticeTask(int TaskId,PracticeDto practice);
    Result<TestCaseDto> UpdateTestCasesAtPracticeTask(int TestId, TestCaseDto testCaseDto);
    Result<List<TestCaseDto>> AddTestCasesAtPracticeTask(List<TestCaseDto> testCaseDtos);
    //Result<PracticeDto> DeletePracticeTask(int TaskId);
    Task<Result<CodeSubmitResponseDto>> SubmitCode(int PlayerId, CodeSubmitRequestDto code);
}
