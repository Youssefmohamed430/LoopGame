using LoopGame.Domain.Entities.Audit;

namespace LoopGame.Tests.Entities;

public class NarrativeGenerationLogTests
{
    [Fact]
    public void DefaultConstructor_InitializesExpectedDefaults()
    {
        var before = DateTime.UtcNow;
        var log = new NarrativeGenerationLog();
        var after = DateTime.UtcNow;

        Assert.Equal(0, log.Id);
        Assert.Equal(0, log.PlayerId);
        Assert.Equal(0, log.ShiftId);
        Assert.Equal(string.Empty, log.OperationType);
        Assert.Equal(string.Empty, log.ModelName);
        Assert.Equal(0, log.ResponseTimeMs);
        Assert.False(log.ValidationPassed);
        Assert.False(log.RepairAttempted);
        Assert.Null(log.RepairSucceeded);
        Assert.False(log.UsedFallback);
        Assert.Null(log.FailureReason);
        Assert.Equal(DateTimeKind.Utc, log.CreatedAt.Kind);
        Assert.InRange(log.CreatedAt, before, after);
        Assert.Null(log.StoryBeat);
    }

    [Fact]
    public void Constants_MatchConfiguredConstraints()
    {
        Assert.Equal(50, NarrativeGenerationLog.MaxOperationTypeLength);
        Assert.Equal(100, NarrativeGenerationLog.MaxModelNameLength);
        Assert.Equal(500, NarrativeGenerationLog.MaxFailureReasonLength);
        Assert.Equal(90, NarrativeGenerationLog.RetentionDays);
    }

    [Fact]
    public void ObjectInitializer_StoresOperationalTelemetryWithoutPromptData()
    {
        var log = new NarrativeGenerationLog
        {
            PlayerId = 12,
            ShiftId = 2,
            OperationType = "ImmediateBranch",
            ModelName = "gemini-2.5-flash",
            ResponseTimeMs = 420,
            ValidationPassed = false,
            RepairAttempted = true,
            RepairSucceeded = true,
            UsedFallback = false
        };

        Assert.Equal(12, log.PlayerId);
        Assert.Equal(2, log.ShiftId);
        Assert.Equal("ImmediateBranch", log.OperationType);
        Assert.Equal("gemini-2.5-flash", log.ModelName);
        Assert.Equal(420, log.ResponseTimeMs);
        Assert.False(log.ValidationPassed);
        Assert.True(log.RepairAttempted);
        Assert.True(log.RepairSucceeded);
        Assert.False(log.UsedFallback);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public void SetFailureReason_NullOrWhitespace_SetsNull(string? reason)
    {
        var log = new NarrativeGenerationLog();
        log.SetFailureReason("PreviousDiagnosticError");

        log.SetFailureReason(reason);

        Assert.Null(log.FailureReason);
    }

    [Fact]
    public void SetFailureReason_TrimsAndReplacesNewlinesWithSpaces()
    {
        var log = new NarrativeGenerationLog();

        log.SetFailureReason("  ERR_SCHEMA_INVALID\r\nMissing field: beat_key\nCode=422  ");

        Assert.Equal("ERR_SCHEMA_INVALID  Missing field: beat_key Code=422", log.FailureReason);
        Assert.DoesNotContain("\r", log.FailureReason);
        Assert.DoesNotContain("\n", log.FailureReason);
    }

    [Fact]
    public void SetFailureReason_Exact500Characters_IsPreserved()
    {
        var log = new NarrativeGenerationLog();
        var exact = new string('E', NarrativeGenerationLog.MaxFailureReasonLength);

        log.SetFailureReason(exact);

        Assert.NotNull(log.FailureReason);
        Assert.Equal(NarrativeGenerationLog.MaxFailureReasonLength, log.FailureReason.Length);
        Assert.Equal(exact, log.FailureReason);
    }

    [Fact]
    public void SetFailureReason_Exceeding500Characters_TruncatesTo500AfterSanitizing()
    {
        var log = new NarrativeGenerationLog();
        var over = "  ERR_TIMEOUT:\n" + new string('X', 600);

        log.SetFailureReason(over);

        Assert.NotNull(log.FailureReason);
        Assert.Equal(NarrativeGenerationLog.MaxFailureReasonLength, log.FailureReason.Length);
        Assert.StartsWith("ERR_TIMEOUT: ", log.FailureReason);
        Assert.DoesNotContain("\n", log.FailureReason);
    }
}
