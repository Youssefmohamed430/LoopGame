namespace LoopGame.Domain.Constants;

/// <summary>
/// Daily Sahm AI hint limits per subscription tier.
/// </summary>
public static class HintLimits
{
    public const int Free       = 3;
    public const int Pro        = 10;
    public const int Team       = 25;
    public const int Enterprise = int.MaxValue; // unlimited
}
