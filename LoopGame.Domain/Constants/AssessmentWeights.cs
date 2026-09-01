namespace LoopGame.Domain.Constants;

/// <summary>
/// Assessment evidence weights used by the mastery calculation algorithm.
/// Centralised here so they can be adjusted without modifying service logic.
/// </summary>
public static class AssessmentWeights
{
    // ── Positive evidence ──────────────────────────────────────────
    /// <summary>
    /// Progression/telemetry event indicating a gate was cleared.
    /// Set to 0.0: GateCleared is a progression outcome, NOT independent mastery evidence,
    /// preventing double-counting alongside PracticeAttempt evidence.
    /// </summary>
    public const double GateCleared       = 0.0;
    public const double PracticeIdeal     = 2.5;
    public const double PracticeAcceptable = 2.0;
    public const double PracticeDebt      = 0.5;
    public const double PracticeMistake   = 0.5;
    public const double ChoiceIdeal       = 1.5;
    public const double SideTask          = 2.0;

    // ── Negative evidence ──────────────────────────────────────────
    public const double HintRequest       = -0.3;

    // ── Recency decay ──────────────────────────────────────────────
    /// <summary>Half-life in days for the exponential recency decay.</summary>
    public const double DecayHalfLifeDays = 7.0;

    // ── Sigmoid normalisation ──────────────────────────────────────
    /// <summary>Steepness of the sigmoid curve.</summary>
    public const double SigmoidK          = 1.0;
    /// <summary>Horizontal shift (midpoint) of the sigmoid curve.</summary>
    public const double SigmoidMidpoint   = 5.0;

    /// <summary>
    /// Supported event type identifiers matching the CHK_Assessment_EventType
    /// database constraint.
    /// </summary>
    public static class EventTypes
    {
        public const string ChoiceSubmission     = "choice_submission";
        public const string PracticeAttempt      = "practice_attempt";
        public const string HintRequest          = "hint_request";
        public const string SideTaskSubmission   = "side_task_submission";
        public const string DesktopInteraction   = "desktop_interaction";
        public const string ConsequenceTrigger   = "consequence_trigger";
        public const string GateCleared          = "gate_cleared";
        public const string ShiftCompleted       = "shift_completed";

        /// <summary>All known event types for validation.</summary>
        public static readonly HashSet<string> All =
        [
            ChoiceSubmission,
            PracticeAttempt,
            HintRequest,
            SideTaskSubmission,
            DesktopInteraction,
            ConsequenceTrigger,
            GateCleared,
            ShiftCompleted
        ];
    }
}
