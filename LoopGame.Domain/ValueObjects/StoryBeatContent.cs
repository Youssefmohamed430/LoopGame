namespace LoopGame.Domain.ValueObjects;

/// <summary>
/// Preview of a choice button rendered inside a beat's content.
/// Embedded in StoryBeatContent.Choices list.
/// </summary>
public record BeatChoicePreview(
    [property: JsonPropertyName("index")] int    Index,
    [property: JsonPropertyName("text")]  string Text
);

/// <summary>
/// Deserialized representation of the StoryBeat.content_json JSON column.
/// Contains the full beat payload: text, avatar, sound effects, and choice previews.
/// </summary>
public record StoryBeatContent(
    [property: JsonPropertyName("text")]         string Text,
    [property: JsonPropertyName("avatar")]       string? Avatar,
    [property: JsonPropertyName("sound_effect")] string? SoundEffect,
    [property: JsonPropertyName("choices")]      List<BeatChoicePreview>? Choices
);
