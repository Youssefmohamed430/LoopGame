namespace LoopGame.Tests.Entities;

public class StoryBeatTests
{
    [Fact]
    public void DefaultConstructor_InitializesSharedCanonicalOwnershipDefaults()
    {
        var before = DateTime.UtcNow;
        var beat = new StoryBeat();
        var after = DateTime.UtcNow;

        Assert.Equal(0, beat.BeatId);
        Assert.Equal(0, beat.ShiftId);
        Assert.Null(beat.PlayerId);
        Assert.Equal(string.Empty, beat.BeatKey);
        Assert.Equal(BeatType.Narrative, beat.BeatType);
        Assert.Null(beat.SequenceOrder);
        Assert.Null(beat.SenderName);
        Assert.Null(beat.DesktopEvent);
        Assert.Equal(0m, beat.DelaySeconds);
        Assert.False(beat.HasChoices);
        Assert.False(beat.IsAiGenerated);
        Assert.Null(beat.GenerationLogId);
        Assert.Equal(DateTimeKind.Utc, beat.CreatedAt.Kind);
        Assert.InRange(beat.CreatedAt, before, after);

        Assert.Null(beat.Player);
        Assert.Null(beat.GenerationLog);
        Assert.Null(beat.Consequence);
        Assert.NotNull(beat.Choices);
        Assert.Empty(beat.Choices);
        Assert.NotNull(beat.PlayerSaves);
        Assert.Empty(beat.PlayerSaves);
    }

    [Fact]
    public void LegacyObjectInitializer_WithoutOwnershipFields_RemainsSharedCanonicalBeat()
    {
        var content = new StoryBeatContent("Welcome to Shift 1", "mgr.png", null, null);

        var beat = new StoryBeat
        {
            ShiftId = 1,
            BeatKey = "s1_intro",
            BeatType = BeatType.Narrative,
            SequenceOrder = 1,
            App = BeatApp.WhatsUpp,
            SenderName = "Manager",
            ContentJson = content,
            DelaySeconds = 1.5m,
            HasChoices = false
        };

        Assert.Null(beat.PlayerId);
        Assert.False(beat.IsAiGenerated);
        Assert.Null(beat.GenerationLogId);
        Assert.Equal(1, beat.ShiftId);
        Assert.Equal("s1_intro", beat.BeatKey);
        Assert.Equal(1, beat.SequenceOrder);
        Assert.Equal(content, beat.ContentJson);
    }

    [Fact]
    public void AiGeneratedBeat_TracksPlayerOwnershipAndGenerationLogLink()
    {
        var beat = new StoryBeat
        {
            ShiftId = 2,
            PlayerId = 42,
            BeatKey = "ai_s2_p42_b1",
            BeatType = BeatType.Narrative,
            SequenceOrder = 3,
            App = BeatApp.LoopCode,
            ContentJson = new StoryBeatContent("Dynamic follow-up", null, null, null),
            HasChoices = true,
            IsAiGenerated = true,
            GenerationLogId = 99
        };

        Assert.Equal(42, beat.PlayerId);
        Assert.True(beat.IsAiGenerated);
        Assert.Equal(99, beat.GenerationLogId);
        Assert.True(beat.HasChoices);
    }

    [Fact]
    public void AiGeneratedConsequenceBeat_AllowsNullSequenceOrderAndRetainsPlayerIdWhenLogCleared()
    {
        var beat = new StoryBeat
        {
            ShiftId = 3,
            PlayerId = 7,
            BeatKey = "ai_s3_p7_conseq_1",
            BeatType = BeatType.Consequence,
            SequenceOrder = null,
            App = BeatApp.MailLoop,
            ContentJson = new StoryBeatContent("Deferred fallout", null, null, null),
            IsAiGenerated = true,
            GenerationLogId = 15
        };

        // Simulate 90-day NarrativeGenerationLog pruning (OnDelete SetNull)
        beat.GenerationLogId = null;

        Assert.Equal(BeatType.Consequence, beat.BeatType);
        Assert.Null(beat.SequenceOrder);
        Assert.Equal(7, beat.PlayerId);
        Assert.True(beat.IsAiGenerated);
        Assert.Null(beat.GenerationLogId);
    }
}
