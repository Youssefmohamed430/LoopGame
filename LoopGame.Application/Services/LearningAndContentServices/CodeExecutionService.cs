using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Domain.Entities.Code;
using LoopGame.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LoopGame.Application.Services.LearningAndContentServices;

public class CodeExecutionService : ICodeExecutionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CodeExecutionService> _logger;

    public CodeExecutionService(HttpClient httpClient, ILogger<CodeExecutionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<TestCaseResult>> ExecuteAsync(string code, List<TestCase> testCases)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("ExecuteAsync called with empty code.");
            return testCases.Select(tc => new TestCaseResult(tc.TestCaseId, false, "Empty source code", 0)).ToList();
        }

        var requestPayload = new CodeRunnerRequestDto
        {
            Language = "c",
            SourceCode = code,
            TestCases = testCases.Select(tc => new CodeRunnerTestCaseDto
            {
                TestCaseId = tc.TestCaseId,
                Input = tc.TestInput ?? string.Empty,
                ExpectedOutput = tc.ExpectedOutput ?? string.Empty
            }).ToList()
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/execute", requestPayload);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("CodeRunner API returned non-success status code {StatusCode}: {ErrorBody}",
                    response.StatusCode, errorBody);

                return testCases.Select(tc => new TestCaseResult(
                    tc.TestCaseId,
                    false,
                    $"Code execution service error ({response.StatusCode}): {errorBody}",
                    0
                )).ToList();
            }

            var executionResult = await response.Content.ReadFromJsonAsync<CodeRunnerResponseDto>();

            if (executionResult == null || !executionResult.Success)
            {
                var compileErr = executionResult?.CompileError ?? "Compilation or system execution error";
                _logger.LogInformation("Code execution unsuccessful or compilation failed: {CompileError}", compileErr);

                return testCases.Select(tc => new TestCaseResult(
                    tc.TestCaseId,
                    false,
                    compileErr,
                    0
                )).ToList();
            }

            return executionResult.Results.Select(r => new TestCaseResult(
                r.TestCaseId,
                r.Passed,
                r.ActualOutput ?? string.Empty,
                (int)r.ExecutionTimeMs
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with CodeRunner API");
            return testCases.Select(tc => new TestCaseResult(
                tc.TestCaseId,
                false,
                $"System error communicating with CodeRunner service: {ex.Message}",
                0
            )).ToList();
        }
    }

    private class CodeRunnerRequestDto
    {
        [JsonPropertyName("language")]
        public string Language { get; set; } = "c";

        [JsonPropertyName("source_code")]
        public string SourceCode { get; set; } = string.Empty;

        [JsonPropertyName("test_cases")]
        public List<CodeRunnerTestCaseDto> TestCases { get; set; } = new();
    }

    private class CodeRunnerTestCaseDto
    {
        [JsonPropertyName("test_case_id")]
        public int TestCaseId { get; set; }

        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;

        [JsonPropertyName("expected_output")]
        public string ExpectedOutput { get; set; } = string.Empty;
    }

    private class CodeRunnerResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("compile_error")]
        public string? CompileError { get; set; }

        [JsonPropertyName("results")]
        public List<CodeRunnerTestCaseResultDto> Results { get; set; } = new();
    }

    private class CodeRunnerTestCaseResultDto
    {
        [JsonPropertyName("test_case_id")]
        public int TestCaseId { get; set; }

        [JsonPropertyName("passed")]
        public bool Passed { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("actual_output")]
        public string ActualOutput { get; set; } = string.Empty;

        [JsonPropertyName("execution_time_ms")]
        public long ExecutionTimeMs { get; set; }

        [JsonPropertyName("exit_code")]
        public int? ExitCode { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
