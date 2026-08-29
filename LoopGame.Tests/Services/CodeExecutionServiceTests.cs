using CodeRunner.Models;
using CodeRunner.Options;
using CodeRunner.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LoopGame.Tests.Services;

public class CodeExecutionServiceTests
{
    private readonly Mock<ISandboxService> _sandboxServiceMock;
    private readonly Mock<ILogger<CodeExecutionService>> _loggerMock;
    private readonly IOptions<CodeRunnerOptions> _options;
    private readonly CodeExecutionService _service;

    public CodeExecutionServiceTests()
    {
        _sandboxServiceMock = new Mock<ISandboxService>();
        _loggerMock = new Mock<ILogger<CodeExecutionService>>();
        _options = Microsoft.Extensions.Options.Options.Create(new CodeRunnerOptions());
        _service = new CodeExecutionService(_sandboxServiceMock.Object, _options, _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_CompilationError_ReturnsCompilationErrorAndNoResults()
    {
        // Arrange
        _sandboxServiceMock.Setup(s => s.CreateSandboxAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("sandbox-123");

        _sandboxServiceMock.Setup(s => s.CompileAsync("sandbox-123", "source.c", "program", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SandboxProcessResult(1, "", "main.c:2:1: error: expected ';' before '}' token", false, 120));

        var request = new ExecuteCodeRequest
        {
            Language = "c",
            SourceCode = "#include <stdio.h>\nint main() { return 0 }",
            TestCases = new List<TestCaseRequest>
            {
                new() { TestCaseId = 1, Input = "", ExpectedOutput = "0" }
            }
        };

        // Act
        var response = await _service.ExecuteAsync(request);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(ExecutionStatus.CompilationError, response.Status);
        Assert.Contains("error: expected ';'", response.CompileError);
        Assert.Empty(response.Results);

        _sandboxServiceMock.Verify(s => s.RunAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        _sandboxServiceMock.Verify(s => s.DestroySandboxAsync("sandbox-123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SuccessfulCompilationAndExecution_ReturnsResults()
    {
        // Arrange
        _sandboxServiceMock.Setup(s => s.CreateSandboxAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("sandbox-123");

        _sandboxServiceMock.Setup(s => s.CompileAsync("sandbox-123", "source.c", "program", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SandboxProcessResult(0, "", "", false, 50));

        _sandboxServiceMock.Setup(s => s.RunAsync("sandbox-123", "program", "5", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SandboxProcessResult(0, "10\n", "", false, 8));

        var request = new ExecuteCodeRequest
        {
            Language = "c",
            SourceCode = "#include <stdio.h>\nint main() { int x; scanf(\"%d\", &x); printf(\"%d\\n\", x*2); return 0; }",
            TestCases = new List<TestCaseRequest>
            {
                new() { TestCaseId = 1, Input = "5", ExpectedOutput = "10" }
            }
        };

        // Act
        var response = await _service.ExecuteAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(ExecutionStatus.Completed, response.Status);
        Assert.Null(response.CompileError);
        Assert.Single(response.Results);
        Assert.True(response.Results[0].Passed);
        Assert.Equal(TestCaseStatus.Passed, response.Results[0].Status);
        Assert.Equal("10\n", response.Results[0].ActualOutput);
    }

    [Theory]
    [InlineData("10\r\n", "10\n", true)]
    [InlineData("10   \n", "10", true)]
    [InlineData("10", "11", false)]
    public void NormalizeOutput_HandlesLineEndingsAndWhitespace(string actual, string expected, bool expectedMatch)
    {
        var normActual = CodeExecutionService.NormalizeOutput(actual);
        var normExpected = CodeExecutionService.NormalizeOutput(expected);

        Assert.Equal(expectedMatch, string.Equals(normActual, normExpected, StringComparison.Ordinal));
    }

    [Fact]
    public void EvaluateTestCaseResult_Timeout_ReturnsTimeoutStatus()
    {
        var testCase = new TestCaseRequest { TestCaseId = 1, Input = "", ExpectedOutput = "ok" };
        var runResult = new SandboxProcessResult(-1, "", "", true, 5000);

        var result = CodeExecutionService.EvaluateTestCaseResult(testCase, runResult, TimeSpan.FromSeconds(5));

        Assert.False(result.Passed);
        Assert.Equal(TestCaseStatus.Timeout, result.Status);
        Assert.Equal("Execution exceeded the allowed timeout.", result.Error);
    }

    [Fact]
    public void EvaluateTestCaseResult_RuntimeError_ReturnsRuntimeErrorStatus()
    {
        var testCase = new TestCaseRequest { TestCaseId = 1, Input = "", ExpectedOutput = "ok" };
        var runResult = new SandboxProcessResult(139, "", "Segmentation fault", false, 15);

        var result = CodeExecutionService.EvaluateTestCaseResult(testCase, runResult, TimeSpan.FromSeconds(5));

        Assert.False(result.Passed);
        Assert.Equal(TestCaseStatus.RuntimeError, result.Status);
        Assert.Equal("Segmentation fault", result.Error);
        Assert.Equal(139, result.ExitCode);
    }
}
