using System.Text.Json.Serialization;

namespace CodeRunner.Models;

public class ExecuteCodeResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("compile_error")]
    public string? CompileError { get; set; }

    [JsonPropertyName("results")]
    public List<TestCaseResult> Results { get; set; } = new();
}

public static class ExecutionStatus
{
    public const string Completed = "Completed";
    public const string CompilationError = "CompilationError";
    public const string Timeout = "Timeout";
    public const string SystemError = "SystemError";
}
