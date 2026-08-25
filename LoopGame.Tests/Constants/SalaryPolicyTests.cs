using LoopGame.Domain.Constants;

namespace LoopGame.Tests.Constants;

public class SalaryPolicyTests
{
    [Theory]
    [InlineData(PlayerRank.Intern, 2000)]
    [InlineData(PlayerRank.Fresh, 3500)]
    [InlineData(PlayerRank.ExperiencedJunior, 5500)]
    [InlineData(PlayerRank.Senior, 8000)]
    [InlineData(PlayerRank.Lead, 12000)]
    public void BaseSalary_MapsEachRankToItsSalaryTier(PlayerRank rank, decimal expected)
    {
        Assert.Equal(expected, SalaryPolicy.BaseSalary(rank));
    }

    [Fact]
    public void ComputeShiftBonus_BlendOfIdealAndAcceptable()
    {
        // base 2000; 10 choices: 5 Ideal (50%), 3 Acceptable (30%), 2 Debt (0%)
        // bonusRate = 0.50*0.20 + 0.30*0.10 = 0.13 → bonus = 260
        var tiers = new Dictionary<ChoiceTier, int>
        {
            [ChoiceTier.Ideal] = 5,
            [ChoiceTier.Acceptable] = 3,
            [ChoiceTier.Debt] = 2
        };

        Assert.Equal(260m, SalaryPolicy.ComputeShiftBonus(2000m, tiers));
    }

    [Fact]
    public void ComputeShiftBonus_AllIdeal_GivesTwentyPercent()
    {
        var tiers = new Dictionary<ChoiceTier, int> { [ChoiceTier.Ideal] = 4 };

        Assert.Equal(700m, SalaryPolicy.ComputeShiftBonus(3500m, tiers)); // 20% of 3500
    }

    [Fact]
    public void ComputeShiftBonus_AllMistakeOrDebt_IsZero()
    {
        var tiers = new Dictionary<ChoiceTier, int>
        {
            [ChoiceTier.Debt] = 3,
            [ChoiceTier.Mistake] = 2
        };

        Assert.Equal(0m, SalaryPolicy.ComputeShiftBonus(5500m, tiers));
    }

    [Fact]
    public void ComputeShiftBonus_EmptyTierCounts_IsZero()
    {
        Assert.Equal(0m, SalaryPolicy.ComputeShiftBonus(8000m, new Dictionary<ChoiceTier, int>()));
    }

    [Fact]
    public void ComputeShiftBonus_RoundsToTwoDecimals()
    {
        // base 1234.55; 1 choice: ideal → rate 0.20 → 246.91 exactly
        // use 3 choices: 1 ideal (1/3) → rate = 0.0666.. → 82.30333.. → 82.30
        var tiers = new Dictionary<ChoiceTier, int>
        {
            [ChoiceTier.Ideal] = 1,
            [ChoiceTier.Mistake] = 2
        };

        Assert.Equal(82.30m, SalaryPolicy.ComputeShiftBonus(1234.55m, tiers));
    }

    [Fact]
    public void ComputeShiftBonus_ZeroBaseSalary_IsZero()
    {
        var tiers = new Dictionary<ChoiceTier, int> { [ChoiceTier.Ideal] = 1 };

        Assert.Equal(0m, SalaryPolicy.ComputeShiftBonus(0m, tiers));
    }
}
