using System.Text.Json.Serialization;

namespace LoopGame.Models;

/// <summary>
/// Unified API error payload for both domain Result failures and model validation.
/// Frontend can always read <see cref="Code"/> + <see cref="Description"/>;
/// <see cref="Errors"/> is present for field-level validation only.
/// </summary>
public sealed class ApiErrorResponse
{
    public string Code { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Field → messages map (model validation). Omitted for domain errors.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; init; }

    public static ApiErrorResponse FromDomain(Error error) => new()
    {
        Code = error.Code,
        Description = error.Description
    };

    public static ApiErrorResponse FromModelState(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState)
    {
        var errors = modelState
            .Where(kvp => kvp.Value is { Errors.Count: > 0 })
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage)
                        ? "Invalid value."
                        : e.ErrorMessage)
                    .ToArray());

        var description = errors.Values.SelectMany(m => m).FirstOrDefault()
            ?? "One or more validation errors occurred.";

        return new ApiErrorResponse
        {
            Code = "Validation.Failed",
            Description = description,
            Errors = errors
        };
    }
}
