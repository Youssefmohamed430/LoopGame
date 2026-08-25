namespace LoopGame.Domain.Abstractions;

public static class SahmErrors
{
    public static readonly Error DailyHintLimitReached = new("Sahm.DailyHintLimitReached", "The daily hint limit for the current subscription tier has been reached.");
    public static readonly Error InvalidTierUpgrade    = new("Sahm.InvalidTierUpgrade", "The requested subscription tier upgrade is invalid.");
}
