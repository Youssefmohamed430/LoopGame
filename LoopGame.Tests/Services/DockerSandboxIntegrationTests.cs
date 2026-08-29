using CodeRunner.Models;
using CodeRunner.Options;
using CodeRunner.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LoopGame.Tests.Services;

public class DockerSandboxIntegrationTests
{
    private readonly DockerSandboxService _sandboxService;
    private readonly CodeExecutionService _executionService;

    public DockerSandboxIntegrationTests()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new CodeRunnerOptions
        {
            DockerImage = "shift-c-runner:latest",
            TimeoutSeconds = 2
        });

        _sandboxService = new DockerSandboxService(options, NullLogger<DockerSandboxService>.Instance);
        _executionService = new CodeExecutionService(_sandboxService, options, NullLogger<CodeExecutionService>.Instance);
    }

    [Fact]
    public async Task RealDocker_ExecuteCProgram_CalculatesDoubleInput()
    {
        var request = new ExecuteCodeRequest
        {
            Language = "c",
            SourceCode = @"
#include <stdio.h>
int main() {
    int n;
    if (scanf(""%d"", &n) == 1) {
        printf(""%d\n"", n * 2);
    }
    return 0;
}",
            TestCases = new List<TestCaseRequest>
            {
                new() { TestCaseId = 1, Input = "5", ExpectedOutput = "10" },
                new() { TestCaseId = 2, Input = "10", ExpectedOutput = "20" }
            }
        };

        var response = await _executionService.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Equal(ExecutionStatus.Completed, response.Status);
        Assert.Equal(2, response.Results.Count);
        Assert.True(response.Results[0].Passed);
        Assert.True(response.Results[1].Passed);
        Assert.Equal("10", response.Results[0].ActualOutput.Trim());
        Assert.Equal("20", response.Results[1].ActualOutput.Trim());
    }

    [Fact]
    public async Task RealDocker_ExecuteInvalidCCode_ReturnsCompilationError()
    {
        var request = new ExecuteCodeRequest
        {
            Language = "c",
            SourceCode = "int main() { invalid_syntax_here; }",
            TestCases = new List<TestCaseRequest>
            {
                new() { TestCaseId = 1, Input = "", ExpectedOutput = "" }
            }
        };

        var response = await _executionService.ExecuteAsync(request);

        Assert.False(response.Success);
        Assert.Equal(ExecutionStatus.CompilationError, response.Status);
        Assert.NotNull(response.CompileError);
        Assert.Contains("error:", response.CompileError);
        Assert.Empty(response.Results);
    }

    [Fact]
    public async Task RealDocker_InfiniteLoop_TimesOutAndCleansUp()
    {
        var request = new ExecuteCodeRequest
        {
            Language = "c",
            SourceCode = @"
#include <stdio.h>
int main() {
    while(1) {}
    return 0;
}",
            TestCases = new List<TestCaseRequest>
            {
                new() { TestCaseId = 1, Input = "", ExpectedOutput = "done" }
            }
        };

        var response = await _executionService.ExecuteAsync(request);

        Assert.True(response.Success);
        Assert.Equal(ExecutionStatus.Completed, response.Status);
        Assert.Single(response.Results);
        Assert.False(response.Results[0].Passed);
        Assert.Equal(TestCaseStatus.Timeout, response.Results[0].Status);
        Assert.Equal("Execution exceeded the allowed timeout.", response.Results[0].Error);
    }
}
