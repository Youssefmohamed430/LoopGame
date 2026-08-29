namespace LoopGame.Application.IServices.LearningAndContentServices;

public interface ICodeExecutionService
{
    Task<List<TestCaseResult>> ExecuteAsync(
        string code,
        List<TestCase> testCases);
}
