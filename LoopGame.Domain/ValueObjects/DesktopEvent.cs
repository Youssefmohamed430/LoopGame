namespace LoopGame.Domain.ValueObjects;

/// <summary>
/// Deserialized representation of the StoryBeat.desktop_event JSON column.
/// Describes a LoopOS desktop side-effect triggered when the beat fires.
/// </summary>
public record DesktopEvent(
    [property: JsonPropertyName("event_type")]           string EventType,
    [property: JsonPropertyName("app_name")]             string? AppName,
    [property: JsonPropertyName("notification_title")]   string? NotificationTitle,
    [property: JsonPropertyName("payload")]              Dictionary<string, object>? Payload
);
