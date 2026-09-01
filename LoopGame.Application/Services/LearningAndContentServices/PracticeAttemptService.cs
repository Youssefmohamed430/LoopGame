using System.Text.Json;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Domain.Entities.Code;
using LoopGame.Domain.Enums;
using LoopGame.Domain.ValueObjects;

namespace LoopGame.Application.Services.LearningAndContentServices;

/// <summary>
/// Stages a PracticeAttempt entity in the repository.
/// Does NOT call SaveAsync — the orchestrator (PracticeService) commits
/// all staged changes (Attempt + Progress) together in one UoW transaction.
/// </summary>
public sealed class PracticeAttemptService(IUnitOfWork _uow) : IPracticeAttemptService
{
    public async Task<int> RecordAttemptAsync(
        int playerId,
        int taskId,
        string submittedCode,
        ChoiceTier tier,
        IReadOnlyList<TestCaseResult> testResults,
        bool hintUsed,
        int timeSpentSec,
        CancellationToken ct = default)
    {
        var attempt = new PracticeAttempt
        {
            PlayerId      = playerId,
            TaskId        = taskId,
            SubmittedCode = submittedCode,
            Tier          = tier,
            TestResults   = JsonSerializer.Serialize(testResults),
            HintUsed      = hintUsed,
            TimeSpentSec  = timeSpentSec
        };

        await _uow.GetRepository<PracticeAttempt>().AddAsync(attempt);

        return attempt.AttemptId;
    }
}
