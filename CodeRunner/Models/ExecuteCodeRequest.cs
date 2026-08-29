using System.Text.Json.Serialization;

namespace CodeRunner.Models;

public class ExecuteCodeRequest
{
    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("source_code")]
    public string SourceCode { get; set; } = string.Empty;

    [JsonPropertyName("test_cases")]
    public List<TestCaseRequest> TestCases { get; set; } = new();
}
