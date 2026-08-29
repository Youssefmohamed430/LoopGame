using CodeRunner.Models;
using CodeRunner.Options;
using Microsoft.Extensions.Options;

namespace CodeRunner.Services;

public class CodeExecutionService : ICodeExecutionService
{
    private readonly ISandboxService _sandboxService;
    private readonly CodeRunnerOptions _options;
    private readonly ILogger<CodeExecutionService> _logger;

    public CodeExecutionService(
        ISandboxService sandboxService,
        IOptions<CodeRunnerOptions> options,
        ILogger<CodeExecutionService> logger)
    {
        _sandboxService = sandboxService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ExecuteCodeResponse> ExecuteAsync(ExecuteCodeRequest request, CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogInformation("[{ExecutionId}] Received code execution request for language '{Language}' with {TestCaseCount} test cases",
            executionId, request.Language, request.TestCases?.Count ?? 0);

        if (!string.Equals(request.Language, "c", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("[{ExecutionId}] Unsupported language requested: {Language}", executionId, request.Language);
            return new ExecuteCodeResponse
            {
                Success = false,
                Status = ExecutionStatus.SystemError,
                CompileError = $"Language '{request.Language}' is not supported. Only 'c' is supported.",
                Results = new()
            };
        }

        if (string.IsNullOrWhiteSpace(request.SourceCode))
        {
            return new ExecuteCodeResponse
            {
                Success = false,
                Status = ExecutionStatus.CompilationError,
                CompileError = "Source code cannot be empty.",
                Results = new()
            };
        }

        string? sandboxId = null;
        try
        {
            // 1. Create temporary Docker sandbox container
            sandboxId = await _sandboxService.CreateSandboxAsync(cancellationToken);

            // 2. Write source code file
            await _sandboxService.WriteFileAsync(sandboxId, "source.c", request.SourceCode, cancellationToken);

            // 3. Compile C program
            var compileResult = await _sandboxService.CompileAsync(sandboxId, "source.c", "program", cancellationToken);

            if (compileResult.ExitCode != 0 || compileResult.IsTimeout)
            {
                var compileErr = !string.IsNullOrWhiteSpace(compileResult.Stderr)
                    ? compileResult.Stderr
                    : compileResult.Stdout;

                _logger.LogInformation("[{ExecutionId}] Compilation failed for request", executionId);
                return new ExecuteCodeResponse
                {
                    Success = false,
                    Status = ExecutionStatus.CompilationError,
                    CompileError = string.IsNullOrWhiteSpace(compileErr) ? "Compilation failed." : compileErr.Trim(),
                    Results = new()
                };
            }

            _logger.LogInformation("[{ExecutionId}] Compilation succeeded. Running test cases...", executionId);

            // 4. Run test cases
            var testCaseResults = new List<TestCaseResult>();
            var timeoutSpan = TimeSpan.FromSeconds(_options.TimeoutSeconds);

            foreach (var testCase in request.TestCases ?? new())
            {
                _logger.LogDebug("[{ExecutionId}] Executing Test Case {TestCaseId}", executionId, testCase.TestCaseId);

                var runResult = await _sandboxService.RunAsync(sandboxId, "program", testCase.Input ?? string.Empty, timeoutSpan, cancellationToken);

                var testResult = EvaluateTestCaseResult(testCase, runResult, timeoutSpan);
                testCaseResults.Add(testResult);

                _logger.LogInformation("[{ExecutionId}] Test Case {TestCaseId} completed with status '{Status}' in {Time}ms",
                    executionId, testCase.TestCaseId, testResult.Status, testResult.ExecutionTimeMs);
            }

            return new ExecuteCodeResponse
            {
                Success = true,
                Status = ExecutionStatus.Completed,
                CompileError = null,
                Results = testCaseResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ExecutionId}] Unexpected exception during code execution execution workflow", executionId);
            return new ExecuteCodeResponse
            {
                Success = false,
                Status = ExecutionStatus.SystemError,
                CompileError = $"System execution error: {ex.Message}",
                Results = new()
            };
        }
        finally
        {
            // Reliable sandbox cleanup guarantee
            if (!string.IsNullOrEmpty(sandboxId))
            {
                await _sandboxService.DestroySandboxAsync(sandboxId, CancellationToken.None);
            }
        }
    }

    public static TestCaseResult EvaluateTestCaseResult(TestCaseRequest testCase, SandboxProcessResult runResult, TimeSpan timeout)
    {
        if (runResult.IsTimeout)
        {
            return new TestCaseResult
            {
                TestCaseId = testCase.TestCaseId,
                Passed = false,
                Status = TestCaseStatus.Timeout,
                ActualOutput = string.Empty,
                ExecutionTimeMs = (long)timeout.TotalMilliseconds,
                ExitCode = null,
                Error = "Execution exceeded the allowed timeout."
            };
        }

        if (runResult.ExitCode != 0)
        {
            var errMessage = !string.IsNullOrWhiteSpace(runResult.Stderr)
                ? runResult.Stderr.Trim()
                : $"Process exited with code {runResult.ExitCode}";

            return new TestCaseResult
            {
                TestCaseId = testCase.TestCaseId,
                Passed = false,
                Status = TestCaseStatus.RuntimeError,
                ActualOutput = runResult.Stdout,
                ExecutionTimeMs = runResult.ElapsedMilliseconds,
                ExitCode = runResult.ExitCode,
                Error = errMessage
            };
        }

        var normalizedActual = NormalizeOutput(runResult.Stdout);
        var normalizedExpected = NormalizeOutput(testCase.ExpectedOutput);

        bool isMatch = string.Equals(normalizedActual, normalizedExpected, StringComparison.Ordinal);

        return new TestCaseResult
        {
            TestCaseId = testCase.TestCaseId,
            Passed = isMatch,
            Status = isMatch ? TestCaseStatus.Passed : TestCaseStatus.WrongAnswer,
            ActualOutput = runResult.Stdout,
            ExecutionTimeMs = runResult.ElapsedMilliseconds,
            ExitCode = runResult.ExitCode,
            Error = null
        };
    }

    public static string NormalizeOutput(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // Standardize line endings CRLF -> LF
        var text = input.Replace("\r\n", "\n").Replace("\r", "\n");

        // Trim trailing spaces per line and trim trailing newlines
        var lines = text.Split('\n').Select(line => line.TrimEnd());
        return string.Join("\n", lines).TrimEnd();
    }
}
