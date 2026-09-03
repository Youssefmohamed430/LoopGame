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
        ChoiceTier tier,
        IReadOnlyList<TestCaseResult> testResults,
        CodeSubmitRequestDto code,
        CancellationToken ct = default)
    {
        var attempt = new PracticeAttempt
        {
            PlayerId      = playerId,
            TaskId        = code.TaskId,
            SubmittedCode = code.SubmittedCode,
            Tier          = tier,
            TestResults   = JsonSerializer.Serialize(testResults),
            HintUsed      = code.HintUsed,
            TimeSpentSec  = code.TimeSpentSec
        };

        await _uow.GetRepository<PracticeAttempt>().AddAsync(attempt);

        return attempt.AttemptId;
    }
}
