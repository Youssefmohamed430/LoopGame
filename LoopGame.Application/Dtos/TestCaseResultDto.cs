namespace LoopGame.Application.Dtos;
    public class TestCaseResultDto
    {
        public int TestCaseId { get; set; }

        public bool Passed { get; set; }

        public string? ActualOutput { get; set; }
    }
