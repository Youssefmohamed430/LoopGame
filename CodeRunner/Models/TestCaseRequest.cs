using System.Text.Json.Serialization;

namespace CodeRunner.Models;

public class TestCaseRequest
{
    [JsonPropertyName("test_case_id")]
    public int TestCaseId { get; set; }

    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    [JsonPropertyName("expected_output")]
    public string ExpectedOutput { get; set; } = string.Empty;
}
