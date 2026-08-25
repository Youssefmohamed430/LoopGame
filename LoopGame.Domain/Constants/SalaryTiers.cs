namespace LoopGame.Domain.Constants;

/// <summary>
/// Base salary amounts (EGP) per player rank tier.
/// </summary>
public static class SalaryTiers
{
    public const decimal Intern            = 2_000m;
    public const decimal Fresh             = 3_500m;
    public const decimal ExperiencedJunior = 5_500m;
    public const decimal Senior            = 8_000m;
    public const decimal Lead              = 12_000m;
}
