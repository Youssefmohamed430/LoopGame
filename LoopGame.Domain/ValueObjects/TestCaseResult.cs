namespace LoopGame.Domain.ValueObjects;

/// <summary>
/// Result of a single test case execution.
/// Used as elements in the PracticeAttempt.test_results and SideTaskSubmission.test_results JSON arrays.
/// </summary>
public record TestCaseResult(
    [property: JsonPropertyName("test_case_id")]      int    TestCaseId,
    [property: JsonPropertyName("passed")]             bool   Passed,
    [property: JsonPropertyName("actual_output")]      string ActualOutput,
    [property: JsonPropertyName("execution_time_ms")]  int    ExecutionTimeMs
);
