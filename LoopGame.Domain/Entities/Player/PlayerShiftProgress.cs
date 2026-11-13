namespace LoopGame.Domain.Entities.Player;

/// <summary>
/// Tracks a player's progression state through a single shift.
/// Composite unique key: (PlayerId, ShiftId).
/// </summary>
public class PlayerShiftProgress
{
    public const int MaxSessionContextLength = 600;
    public const int MaxSessionContextSentences = 2;

    public int                ProgressId     { get; set; }
    public int                PlayerId       { get; set; }
    public int                ShiftId        { get; set; }
    public ShiftProgressStatus Status        { get; set; } = ShiftProgressStatus.InProgress;
    
    /// <summary>Indicates whether the shift's mandatory practice gate has been cleared.</summary>
    public bool               IsGateCleared  { get; set; } = false;

    /// <summary>Timestamp when the gate was cleared.</summary>
    public DateTime?          GateClearedAt  { get; set; }

    public DateTime?          StartedAt      { get; set; }
    public DateTime?          CompletedAt    { get; set; }
    public short              GateAttempts   { get; set; } = 0;

    /// <summary>
    /// Compact rolling narrative summary (max 2 sentences / ~120 tokens / 600 chars)
    /// carried across AI narrative generation calls within the shift.
    /// </summary>
    public string             SessionContext { get; set; } = string.Empty;

    // Navigation
    public Player Player { get; set; }  = null!;
    public Shift  Shift  { get; set; }  = null!;

    /// <summary>
    /// Trims, normalizes whitespace, and enforces the 2-sentence / 600-character limit safely.
    /// </summary>
    public void UpdateSessionContext(string? context)
    {
        SessionContext = NormalizeSessionContext(context);
    }

    public static string NormalizeSessionContext(string? context)
    {
        if (string.IsNullOrWhiteSpace(context))
            return string.Empty;

        var normalized = CollapseWhitespace(context.Trim());
        if (normalized.Length == 0)
            return string.Empty;

        var sentenceEnd = FindNthSentenceEnd(normalized, MaxSessionContextSentences);
        if (sentenceEnd >= 0 && sentenceEnd < normalized.Length)
            normalized = normalized[..sentenceEnd].TrimEnd();

        if (normalized.Length <= MaxSessionContextLength)
            return normalized;

        return TruncateSafely(normalized, MaxSessionContextLength);
    }

    private static string CollapseWhitespace(string input)
    {
        var sb = new System.Text.StringBuilder(input.Length);
        bool prevSpace = false;

        foreach (char ch in input)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(ch);
                prevSpace = false;
            }
        }

        return sb.ToString();
    }

    private static int FindNthSentenceEnd(string text, int maxSentences)
    {
        int sentences = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch is '.' or '!' or '?')
            {
                bool isBoundary = i == text.Length - 1 || char.IsWhiteSpace(text[i + 1]);
                if (isBoundary)
                {
                    sentences++;
                    if (sentences >= maxSentences)
                        return i + 1;
                }
            }
        }

        return -1;
    }

    private static string TruncateSafely(string text, int maxLength)
    {
        int cut = maxLength;

        if (cut > 0 && cut < text.Length && char.IsHighSurrogate(text[cut - 1]))
            cut--;

        int lastSpace = text.LastIndexOf(' ', cut);
        if (lastSpace > maxLength / 2)
            cut = lastSpace;

        return text[..cut].TrimEnd();
    }
}
