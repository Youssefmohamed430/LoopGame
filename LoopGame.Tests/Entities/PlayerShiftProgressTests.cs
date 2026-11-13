namespace LoopGame.Tests.Entities;

public class PlayerShiftProgressTests
{
    [Fact]
    public void DefaultConstructor_InitializesSessionContextToEmptyString()
    {
        var progress = new PlayerShiftProgress();

        Assert.Equal(string.Empty, progress.SessionContext);
        Assert.Equal(ShiftProgressStatus.InProgress, progress.Status);
        Assert.False(progress.IsGateCleared);
        Assert.Equal((short)0, progress.GateAttempts);
        Assert.Null(progress.GateClearedAt);
        Assert.Null(progress.StartedAt);
        Assert.Null(progress.CompletedAt);
    }

    [Fact]
    public void Constants_MatchConfiguredLimits()
    {
        Assert.Equal(600, PlayerShiftProgress.MaxSessionContextLength);
        Assert.Equal(2, PlayerShiftProgress.MaxSessionContextSentences);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n  ")]
    public void UpdateSessionContext_NullOrWhitespace_SetsEmptyString(string? input)
    {
        var progress = new PlayerShiftProgress();
        progress.UpdateSessionContext("Existing context.");

        progress.UpdateSessionContext(input);

        Assert.Equal(string.Empty, progress.SessionContext);
    }

    [Fact]
    public void UpdateSessionContext_NormalizesAndCollapsesWhitespace()
    {
        var progress = new PlayerShiftProgress();

        progress.UpdateSessionContext("  First\t\tsentence. \r\n  Second   sentence!   ");

        Assert.Equal("First sentence. Second sentence!", progress.SessionContext);
    }

    [Fact]
    public void UpdateSessionContext_TwoSentencesWithinLimit_PreservesBothSentences()
    {
        var progress = new PlayerShiftProgress();
        const string input = "Player patched the null pointer bug. QA approved the hotfix?";

        progress.UpdateSessionContext(input);

        Assert.Equal(input, progress.SessionContext);
    }

    [Fact]
    public void UpdateSessionContext_MoreThanTwoSentences_TruncatesAtSecondSentence()
    {
        var progress = new PlayerShiftProgress();
        const string input = "First sentence. Second sentence! Third sentence? Fourth sentence.";

        progress.UpdateSessionContext(input);

        Assert.Equal("First sentence. Second sentence!", progress.SessionContext);
    }

    [Fact]
    public void UpdateSessionContext_DotsInsideTokens_AreNotTreatedAsSentenceBoundaries()
    {
        var progress = new PlayerShiftProgress();
        const string input = "Updated main.c to v1.2 for task. Passed CI checks! Extra third sentence.";

        progress.UpdateSessionContext(input);

        Assert.Equal("Updated main.c to v1.2 for task. Passed CI checks!", progress.SessionContext);
    }

    [Fact]
    public void UpdateSessionContext_Exactly600Characters_IsPreservedWithoutTruncation()
    {
        var progress = new PlayerShiftProgress();
        var exact = new string('a', PlayerShiftProgress.MaxSessionContextLength);

        progress.UpdateSessionContext(exact);

        Assert.Equal(PlayerShiftProgress.MaxSessionContextLength, progress.SessionContext.Length);
        Assert.Equal(exact, progress.SessionContext);
    }

    [Fact]
    public void UpdateSessionContext_Exceeding600CharactersWithSpaces_TruncatesAtWordBoundary()
    {
        var progress = new PlayerShiftProgress();
        var prefix = new string('a', 590);
        var input = $"{prefix} wordone wordtwo wordthree.";

        progress.UpdateSessionContext(input);

        Assert.True(progress.SessionContext.Length <= PlayerShiftProgress.MaxSessionContextLength);
        Assert.Equal($"{prefix} wordone", progress.SessionContext);
    }

    [Fact]
    public void UpdateSessionContext_Exceeding600CharactersWithoutSpaces_CapsAt600Characters()
    {
        var progress = new PlayerShiftProgress();
        var input = new string('x', 650);

        progress.UpdateSessionContext(input);

        Assert.Equal(PlayerShiftProgress.MaxSessionContextLength, progress.SessionContext.Length);
        Assert.Equal(new string('x', 600), progress.SessionContext);
    }

    [Fact]
    public void UpdateSessionContext_SurrogatePairAt600Boundary_DoesNotSplitHighSurrogate()
    {
        var progress = new PlayerShiftProgress();
        // Index 599 is high surrogate, index 600 is low surrogate -> backs off to 599
        var input = new string('a', 599) + "😀" + "tail";

        progress.UpdateSessionContext(input);

        Assert.Equal(599, progress.SessionContext.Length);
        Assert.False(char.IsHighSurrogate(progress.SessionContext[^1]));
    }

    [Fact]
    public void UpdateSessionContext_TwoLongSentencesExceeding600Characters_EnforcesLengthCapAfterSentenceCap()
    {
        var progress = new PlayerShiftProgress();
        var s1 = new string('a', 350) + ".";
        var s2 = " " + new string('b', 350) + ".";
        var s3 = " Should be dropped.";

        progress.UpdateSessionContext(s1 + s2 + s3);

        Assert.True(progress.SessionContext.Length <= PlayerShiftProgress.MaxSessionContextLength);
        Assert.DoesNotContain("Should be dropped", progress.SessionContext);
    }
}
