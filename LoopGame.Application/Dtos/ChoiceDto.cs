namespace LoopGame.Application.Dtos;
    public class ChoiceDto
    {
        public int ChoiceId { get; set; }
        public int BeatId { get; set; }
        public byte ChoiceIndex { get; set; } // 1–4
        public string ChoiceText { get; set; } = string.Empty;
        public ChoiceTier Tier { get; set; }
        public int? ConsequenceId { get; set; }
        public string? ImmediateFeedback { get; set; }
    }