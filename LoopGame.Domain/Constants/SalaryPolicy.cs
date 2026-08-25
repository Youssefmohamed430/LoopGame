namespace LoopGame.Domain.Constants;

/// <summary>
/// Pure domain policy for shift salary computation (UC-ECO salary flow).
/// No I/O, no clock access — fully deterministic and unit-testable.
/// </summary>
public static class SalaryPolicy
{
    /// <summary>Bonus weight applied to the share of Ideal choices.</summary>
    public const decimal IdealWeight = 0.20m;

    /// <summary>Bonus weight applied to the share of Acceptable choices.</summary>
    public const decimal AcceptableWeight = 0.10m;

    /// <summary>
    /// Base salary (EGP) for a shift, determined by the player's current rank.
    /// </summary>
    public static decimal BaseSalary(PlayerRank rank) => rank switch
    {
        PlayerRank.Intern            => SalaryTiers.Intern,
        PlayerRank.Fresh             => SalaryTiers.Fresh,
        PlayerRank.ExperiencedJunior => SalaryTiers.ExperiencedJunior,
        PlayerRank.Senior            => SalaryTiers.Senior,
        PlayerRank.Lead              => SalaryTiers.Lead,
        _ => throw new ArgumentOutOfRangeException(nameof(rank), rank, "Unknown player rank.")
    };

    /// <summary>
    /// Shift performance bonus:
    ///   bonus = baseSalary × (idealShare × 0.20 + acceptableShare × 0.10)
    /// where idealShare/acceptableShare are the fractions of the shift's choices
    /// rated Ideal / Acceptable. Debt and Mistake tiers contribute nothing.
    /// A shift with no recorded choices yields no bonus. Result rounded to 2 decimals.
    /// </summary>
    public static decimal ComputeShiftBonus(decimal baseSalary, IReadOnlyDictionary<ChoiceTier, int> tierCounts)
    {
        if (baseSalary <= 0 || tierCounts.Count == 0)
            return 0m;

        var totalChoices = 0;
        foreach (var count in tierCounts.Values)
            totalChoices += count;

        if (totalChoices <= 0)
            return 0m;

        var idealShare = tierCounts.GetValueOrDefault(ChoiceTier.Ideal) / (decimal)totalChoices;
        var acceptableShare = tierCounts.GetValueOrDefault(ChoiceTier.Acceptable) / (decimal)totalChoices;

        var bonusRate = idealShare * IdealWeight + acceptableShare * AcceptableWeight;
        return Math.Round(baseSalary * bonusRate, 2, MidpointRounding.AwayFromZero);
    }
}
