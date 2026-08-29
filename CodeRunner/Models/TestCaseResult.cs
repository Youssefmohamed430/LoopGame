using System.Text.Json.Serialization;

namespace CodeRunner.Models;

public class TestCaseResult
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

public static class TestCaseStatus
{
    public const string Passed = "Passed";
    public const string WrongAnswer = "WrongAnswer";
    public const string RuntimeError = "RuntimeError";
    public const string Timeout = "Timeout";
    public const string MemoryLimitExceeded = "MemoryLimitExceeded";
    public const string OutputLimitExceeded = "OutputLimitExceeded";
}
