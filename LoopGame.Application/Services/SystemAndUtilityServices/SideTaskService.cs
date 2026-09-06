using LoopGame.Application.Dtos.SideTaskDtos;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Application.IServices.SystemAndUtilityServices;
using LoopGame.Domain.Entities.Code;
using LoopGame.Domain.Entities.Player;
using LoopGame.Domain.Entities.SideTask;
using LoopGame.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LoopGame.Application.Services.SystemAndUtilityServices;

public class SideTaskService(
    IUnitOfWork _unitOfWork,
    ICodeExecutionService _codeExecutor,
    IEconomyService _economy,
    ILogger<SideTaskService> _logger) : ISideTaskService
{
    public async Task<Result<SideTaskDto>> GetActiveTaskAsync(int playerId, CancellationToken ct = default)
    {
        var task = await _unitOfWork.GetRepository<PlayerSideTask>()
            .FindAll(t => t.PlayerId == playerId && t.Status == SideTaskStatus.Active)
            .Select(t => new
            {
                t.SideTaskId,
                t.ResolvedTitle,
                t.ResolvedDescription,
                t.EgpReward,
                t.Status,
                t.Template.ConceptTag
            })
            .FirstOrDefaultAsync(ct);

        if (task is null)
            return Result.Failure<SideTaskDto>(SideTaskErrors.NoActiveTask);

        return new SideTaskDto(
            task.SideTaskId,
            task.ResolvedTitle,
            task.ResolvedDescription,
            task.EgpReward,
            task.Status.ToString(),
            task.ConceptTag);
    }

    public async Task<Result<CodeSubmitResponseDto>> SubmitSideTaskAsync(
        int playerId, SideTaskSubmitRequestDto dto, CancellationToken ct = default)
    {
        // 1. Load and guard the task.
        var task = _unitOfWork.GetRepository<PlayerSideTask>()
            .FindWithTracking(t => t.SideTaskId == dto.SideTaskId && t.PlayerId == playerId);

        if (task is null)
            return Result.Failure<CodeSubmitResponseDto>(SideTaskErrors.TaskNotFound);

        if (task.Status != SideTaskStatus.Active)
            return Result.Failure<CodeSubmitResponseDto>(SideTaskErrors.TaskAlreadyClosed);

        // 2. Fetch all test cases for the side task.
        var testCases = await _unitOfWork.GetRepository<TestCase>()
            .FindAll(tc => tc.SideTaskId == task.SideTaskId)
            .ToListAsync(ct);

        // 3. Run code execution.
        var executionResults = await _codeExecutor.ExecuteAsync(dto.SubmittedCode, testCases);

        // 4. Compute tier from pass rate.
        int passCount = executionResults.Count(r => r.Passed);
        double passRate = testCases.Count > 0 ? (double)passCount / testCases.Count : 0;

        var tier = passRate == 1.0
            ? ChoiceTier.Ideal
            : passRate >= 0.75 ? ChoiceTier.Acceptable
                : passRate >= 0.5
                    ? ChoiceTier.Debt: ChoiceTier.Mistake;

        // 5. EGP reward multiplier: Ideal=100%, Acceptable=75%, Debt=25%, Mistake=0%.
        decimal multiplier = tier switch
        {
            ChoiceTier.Ideal       => 1.00m,
            ChoiceTier.Acceptable  => 0.75m,
            ChoiceTier.Debt        => 0.25m,
            _                      => 0.00m
        };
        decimal egpEarned = Math.Round(task.EgpReward * multiplier, 2);

        // 6. Persist submission + mark task closed + credit EGP in one transaction.
        var submission = new SideTaskSubmission
        {
            SideTaskId    = dto.SideTaskId,
            PlayerId      = playerId,
            SubmittedCode = dto.SubmittedCode,
            Tier          = tier,
            TestResults   = System.Text.Json.JsonSerializer.Serialize(executionResults),
            SahmHintsUsed = dto.SahmHintsUsed,
            TimeSpentSec  = dto.TimeSpentSec,
            EgpEarned     = egpEarned
        };

        await _unitOfWork.GetRepository<SideTaskSubmission>().AddAsync(submission);
        task.Status      = SideTaskStatus.Submitted;
        task.CompletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveAsync(ct);

        // 7. Credit EGP if earned (economy manages its own transaction).
        if (egpEarned > 0)
        {
            var creditResult = await _economy.ApplyEgpDeltaAsync(
                playerId, egpEarned,
                TransactionType.SideTask,
                $"Side task reward (task #{dto.SideTaskId})",
                referenceId: dto.SideTaskId,
                ct: ct);

            if (creditResult.IsFailure)
            {
                // Log but don't fail the submission — task is already marked Submitted.
                _logger.LogError(
                    "Failed to credit EGP for player {PlayerId} after side task submission {SideTaskId}: {Error}",
                    playerId, dto.SideTaskId, creditResult.Error);
                // The reward can be reconciled later.
            }
        }

        // 8. Activate the next queued task (easiest first by Difficulty).
        await ActivateNextQueuedTaskAsync(playerId, ct);

        var resultDtos = executionResults
            .Select(r => new TestCaseResultDto
            {
                TestCaseId   = r.TestCaseId,
                Passed       = r.Passed,
                ActualOutput = r.ActualOutput
            })
            .ToList();

        return new CodeSubmitResponseDto
        {
            Tier      = tier,
            TestResults = System.Text.Json.JsonSerializer.Serialize(resultDtos),
            EgpEarned = egpEarned
        };
    }

    public async Task<Result<AbandonResultDto>> AbandonTaskAsync(
        int playerId, int sideTaskId, CancellationToken ct = default)
    {
        var task = _unitOfWork.GetRepository<PlayerSideTask>()
            .FindWithTracking(t => t.SideTaskId == sideTaskId && t.PlayerId == playerId);

        if (task is null)
            return Result.Failure<AbandonResultDto>(SideTaskErrors.TaskNotFound);

        if (task.Status != SideTaskStatus.Active)
            return Result.Failure<AbandonResultDto>(SideTaskErrors.TaskAlreadyClosed);

        // Mark abandoned first, then apply penalty via EconomyService.
        task.Status      = SideTaskStatus.Abandoned;
        task.CompletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveAsync(ct);

        var penaltyResult = await _economy.ApplyEgpDeltaAsync(
            playerId,
            -EgpPenalties.Abandonment,
            TransactionType.Penalty,
            $"Abandonment penalty (task #{sideTaskId})",
            referenceId: sideTaskId,
            ct: ct);

        if (penaltyResult.IsFailure)
            return Result.Failure<AbandonResultDto>(penaltyResult.Error);

        // Activate the next queued task (easiest first by Difficulty).
        await ActivateNextQueuedTaskAsync(playerId, ct);

        return new AbandonResultDto(
            PenaltyApplied: -EgpPenalties.Abandonment,
            NewBalance:     penaltyResult.Value);
    }

    public async Task<Result> AssignNewTaskAsync(int playerId, CancellationToken ct = default)
    {
        // Guard: player must not already have an active task.
        var hasActive = await _unitOfWork.GetRepository<PlayerSideTask>()
            .FindAll(t => t.PlayerId == playerId && t.Status == SideTaskStatus.Active)
            .AnyAsync(ct);

        if (hasActive)
            return Result.Failure(SideTaskErrors.AlreadyHasActiveTask);

        // Get player rank to filter eligible templates.
        var rank = await _unitOfWork.GetRepository<Player>()
            .FindAll(p => p.PlayerId == playerId)
            .Select(p => (PlayerRank?)p.Rank)
            .FirstOrDefaultAsync(ct);

        if (rank is null)
            return Result.Failure(EconomyErrors.PlayerNotFound);

        // Pick a random active template within rank requirement.
        var templates = await _unitOfWork.GetRepository<SideTaskTemplate>()
            .FindAll(t => t.IsActive && (int)t.RankRequired <= (int)rank.Value)
            .Select(t => new { t.TemplateId, t.TitleTemplate, t.DescriptionTemplate, t.EgpMin, t.EgpMax })
            .ToListAsync(ct);

        if (templates.Count == 0)
            return Result.Failure(SideTaskErrors.TemplateNotFound);

        var chosen = templates[Random.Shared.Next(templates.Count)];

        var egpRange = (double)(chosen.EgpMax - chosen.EgpMin);
        var egpReward = chosen.EgpMin + (decimal)(Random.Shared.NextDouble() * egpRange);
        egpReward = Math.Round(egpReward, 2);

        var newTask = new PlayerSideTask
        {
            PlayerId            = playerId,
            TemplateId          = chosen.TemplateId,
            ResolvedTitle       = chosen.TitleTemplate,
            ResolvedDescription = chosen.DescriptionTemplate,
            EgpReward           = egpReward,
            Status              = SideTaskStatus.Active,
            AssignedAt          = DateTime.UtcNow
        };

        await _unitOfWork.GetRepository<PlayerSideTask>().AddAsync(newTask);
        await _unitOfWork.SaveAsync(ct);

        return Result.Success();
    }

    public async Task<Result<List<SideTaskHintDto>>> GetHintsAsync(
        int playerId, int sideTaskId, CancellationToken ct = default)
    {
        // Verify task belongs to the player and is active.
        var taskExists = await _unitOfWork.GetRepository<PlayerSideTask>()
            .FindAll(t => t.SideTaskId == sideTaskId
                       && t.PlayerId  == playerId
                       && t.Status    == SideTaskStatus.Active)
            .AnyAsync(ct);

        if (!taskExists)
            return Result.Failure<List<SideTaskHintDto>>(SideTaskErrors.TaskNotFound);

        var hints = await _unitOfWork.GetRepository<SideTaskHint>()
            .FindAll(h => h.SideTaskId == sideTaskId)
            .OrderBy(h => h.HintLevel)
            .Select(h => new SideTaskHintDto(
                h.HintId,
                (int)h.HintLevel,
                h.HintLevel.ToString(),
                h.EgpCost,
                h.IsUnlocked,
                h.IsUnlocked ? h.HintText : null))
            .ToListAsync(ct);

        return hints;
    }

    public async Task<Result<UnlockHintResultDto>> UnlockHintAsync(
        int playerId, UnlockHintRequestDto dto, CancellationToken ct = default)
    {
        // 1. Validate HintLevel is a defined enum value.
        if (!Enum.IsDefined(typeof(HintLevel), dto.HintLevel))
            return Result.Failure<UnlockHintResultDto>(SideTaskErrors.HintNotFound);

        // 2. Guard: task must belong to player and be active.
        var task = _unitOfWork.GetRepository<PlayerSideTask>()
            .FindWithTracking(t => t.SideTaskId == dto.SideTaskId
                               && t.PlayerId   == playerId
                               && t.Status     == SideTaskStatus.Active);

        if (task is null)
            return Result.Failure<UnlockHintResultDto>(SideTaskErrors.TaskNotFound);

        // 3. Load hint (tracked) by level.
        var hintLevel = (HintLevel)dto.HintLevel;
        var hint = _unitOfWork.GetRepository<SideTaskHint>()
            .FindWithTracking(h => h.SideTaskId == dto.SideTaskId
                               && h.HintLevel   == hintLevel);

        if (hint is null)
            return Result.Failure<UnlockHintResultDto>(SideTaskErrors.HintNotFound);

        // 4. Guard: already unlocked.
        if (hint.IsUnlocked)
            return Result.Failure<UnlockHintResultDto>(SideTaskErrors.HintAlreadyUnlocked);

        // 5. Deduct EGP if there is a cost, then mark unlocked — both inside a transaction
        //    so EGP is never deducted without the hint actually being unlocked.
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            decimal newBalance = 0m;
            if (hint.EgpCost > 0)
            {
                // EconomyService.ApplyEgpDeltaAsync opens its own transaction internally,
                // but BeginTransactionAsync is a no-op when one is already active,
                // so both writes share the same DB transaction.
                var deductResult = await _economy.ApplyEgpDeltaAsync(
                    playerId,
                    -hint.EgpCost,
                    TransactionType.Purchase,
                    $"Hint unlock (task #{dto.SideTaskId}, level {dto.HintLevel})",
                    referenceId: hint.HintId,
                    ct: ct);

                if (deductResult.IsFailure)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return Result.Failure<UnlockHintResultDto>(deductResult.Error);
                }

                newBalance = deductResult.Value;
            }
            else
            {
                // Free hint — still return the current balance for the client.
                var balResult = await _economy.GetBalanceAsync(playerId, ct);
                newBalance = balResult.IsSuccess ? balResult.Value.Balance : 0m;
            }

            // 6. Mark as unlocked.
            hint.IsUnlocked = true;
            hint.UnlockedAt = DateTime.UtcNow;
            await _unitOfWork.SaveAsync(ct);
            await _unitOfWork.CommitAsync(ct);

            return new UnlockHintResultDto(
                hint.HintId,
                hint.HintText,
                hint.EgpCost,
                newBalance);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Promotes the next queued task (ordered by Difficulty ascending) to Active.
    /// Called after a task is submitted or abandoned.
    /// </summary>
    private async Task ActivateNextQueuedTaskAsync(int playerId, CancellationToken ct)
    {
        // Find the next queued task with the lowest difficulty.
        var nextTaskId = await _unitOfWork.GetRepository<PlayerSideTask>()
            .FindAll(t => t.PlayerId == playerId && t.Status == SideTaskStatus.Queued)
            .OrderBy(t => t.Difficulty)
            .Select(t => (int?)t.SideTaskId)
            .FirstOrDefaultAsync(ct);

        if (nextTaskId is null)
            return; // No queued tasks remaining.

        var nextTask = _unitOfWork.GetRepository<PlayerSideTask>()
            .FindWithTracking(t => t.SideTaskId == nextTaskId.Value);

        if (nextTask is null)
            return;

        nextTask.Status = SideTaskStatus.Active;
        await _unitOfWork.SaveAsync(ct);
    }
}
