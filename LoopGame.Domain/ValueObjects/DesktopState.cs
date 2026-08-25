namespace LoopGame.Domain.ValueObjects;

/// <summary>
/// Window position and size on the LoopOS desktop. Embedded in DesktopState.
/// </summary>
public record WindowRect(
    [property: JsonPropertyName("x")]      int X,
    [property: JsonPropertyName("y")]      int Y,
    [property: JsonPropertyName("width")]  int Width,
    [property: JsonPropertyName("height")] int Height
);

/// <summary>
/// Deserialized representation of the PlayerSave.desktop_state JSON column.
/// Captures the full LoopOS desktop snapshot for save/load.
/// </summary>
public record DesktopState(
    [property: JsonPropertyName("open_windows")]     List<string> OpenWindows,
    [property: JsonPropertyName("active_window")]    string ActiveWindow,
    [property: JsonPropertyName("wallpaper_id")]     string WallpaperId,
    [property: JsonPropertyName("window_positions")] Dictionary<string, WindowRect> WindowPositions
);
