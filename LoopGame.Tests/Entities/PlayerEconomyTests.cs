namespace LoopGame.Tests.Entities;

public class PlayerEconomyTests
{
    private static PlayerEconomy NewEconomy() => new()
    {
        PlayerId = 1,
        SalaryTier = 1,
    };

    private static PlayerEconomy SeededEconomy(decimal balance)
    {
        var economy = new PlayerEconomy();
        economy.Credit(balance, TransactionType.Bonus, "seed");
        return economy;
    }

    [Fact]
    public void Credit_IncreasesBalance_TotalEarned_AndReturnsLedgerRow()
    {
        var economy = NewEconomy();

        var ledger = economy.Credit(500m, TransactionType.Salary, "Shift salary", referenceId: 7);

        Assert.Equal(500m, economy.Balance);
        Assert.Equal(500m, economy.TotalEarned);
        Assert.Equal(0m, economy.TotalSpent);
        Assert.Equal(500m, ledger.Amount);
        Assert.Equal(500m, ledger.BalanceAfter);
        Assert.Equal(TransactionType.Salary, ledger.TransactionType);
        Assert.Equal("Shift salary", ledger.Description);
        Assert.Equal(7, ledger.ReferenceId);
        Assert.Equal(1, ledger.PlayerId);
    }

    [Fact]
    public void TryDebit_WithSufficientBalance_DebitsAndReturnsSignedLedgerRow()
    {
        var economy = SeededEconomy(300m);

        var result = economy.TryDebit(120m, TransactionType.Purchase, "Camera");

        Assert.True(result.IsSuccess);
        Assert.Equal(180m, economy.Balance);
        Assert.Equal(120m, economy.TotalSpent);
        Assert.Equal(300m, economy.TotalEarned);

        var ledger = result.Value;
        Assert.Equal(-120m, ledger.Amount);          // signed negative for debits
        Assert.Equal(180m, ledger.BalanceAfter);     // post-debit balance snapshot
        Assert.Equal(TransactionType.Purchase, ledger.TransactionType);
    }

    [Fact]
    public void TryDebit_WithInsufficientBalance_FailsAndLeavesStateUntouched()
    {
        var economy = SeededEconomy(50m);

        var result = economy.TryDebit(60m, TransactionType.Purchase, "Too expensive");

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.InsufficientBalance, result.Error);
        Assert.Equal(50m, economy.Balance);   // unchanged
        Assert.Equal(0m, economy.TotalSpent); // unchanged
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void TryDebit_WithNonPositiveAmount_Fails(decimal amount)
    {
        var economy = SeededEconomy(100m);

        var result = economy.TryDebit(amount, TransactionType.Purchase, "Invalid");

        Assert.True(result.IsFailure);
        Assert.Equal(EconomyErrors.InsufficientBalance, result.Error);
        Assert.Equal(100m, economy.Balance);
    }

    [Fact]
    public void ApplyPenalty_ClampsAtZero_NeverGoesNegative()
    {
        var economy = SeededEconomy(40m);

        var ledger = economy.ApplyPenalty(100m, "Abandonment penalty", referenceId: 3);

        Assert.Equal(0m, economy.Balance);            // clamped at zero
        Assert.Equal(40m, economy.TotalSpent);        // only the applied amount counts
        Assert.Equal(-40m, ledger.Amount);            // signed amount actually applied
        Assert.Equal(0m, ledger.BalanceAfter);
        Assert.Equal(TransactionType.Penalty, ledger.TransactionType);
    }

    [Fact]
    public void ApplyPenalty_AppliesFullAmount_WhenBalanceCoversIt()
    {
        var economy = SeededEconomy(200m);

        var ledger = economy.ApplyPenalty(150m, "Penalty");

        Assert.Equal(50m, economy.Balance);
        Assert.Equal(150m, economy.TotalSpent);
        Assert.Equal(-150m, ledger.Amount);
        Assert.Equal(50m, ledger.BalanceAfter);
    }

    [Fact]
    public void Reset_ZeroesBalanceAndTotals_ForNewGameFlow()
    {
        var economy = SeededEconomy(250m);
        economy.TryDebit(50m, TransactionType.Purchase, "Desk item");

        economy.Reset();

        Assert.Equal(0m, economy.Balance);
        Assert.Equal(0m, economy.TotalEarned);
        Assert.Equal(0m, economy.TotalSpent);
    }

    [Fact]
    public void LedgerRows_AreIndependentSnapshots_BalanceAfterIsImmutableHistory()
    {
        var economy = NewEconomy();

        var first = economy.Credit(100m, TransactionType.Bonus, "first");
        var second = economy.Credit(50m, TransactionType.SideTask, "second");

        Assert.Equal(100m, first.BalanceAfter); // history not rewritten by later ops
        Assert.Equal(150m, second.BalanceAfter);
    }
}
