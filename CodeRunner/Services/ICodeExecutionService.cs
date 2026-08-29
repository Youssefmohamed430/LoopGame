using CodeRunner.Models;

namespace CodeRunner.Services;

public interface ICodeExecutionService
{
    Task<ExecuteCodeResponse> ExecuteAsync(ExecuteCodeRequest request, CancellationToken cancellationToken = default);
}
