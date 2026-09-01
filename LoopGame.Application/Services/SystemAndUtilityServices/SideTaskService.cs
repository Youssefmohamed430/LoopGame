//using LoopGame.Application.Dtos.SideTaskDtos;
//using LoopGame.Application.IServices.EconomyAndProgressionServices;

//namespace LoopGame.Application.Services.SystemAndUtilityServices;


//public class SideTaskService(
//    IUnitOfWork _uow,
//    ICodeExecutionService _codeExecutor,
//    IEconomyService _economy) : ISideTaskService
//{
//    public async Task<Result<SideTaskDto>> GetActiveTaskAsync(int playerId, CancellationToken ct = default)
//    {
//        var task = await _uow.GetRepository<PlayerSideTask>()
//            .FindAll(t => t.PlayerId == playerId && t.Status == SideTaskStatus.Active)
//            .Select(t => new
//            {
//                t.SideTaskId,
//                t.ResolvedTitle,
//                t.ResolvedDescription,
//                t.EgpReward,
//                t.DeadlineAt,
//                t.Status,
//                t.AssignedAt,
//                t.Template.ConceptTag
//            })
//            .FirstOrDefaultAsync(ct);

//        if (task is null)
//            return Result.Failure<SideTaskDto>(SideTaskErrors.NoActiveTask);

//        // Lazy expiry check — if deadline passed, mark Expired and return the error.
//        if (task.DeadlineAt.HasValue && task.DeadlineAt.Value < DateTime.UtcNow)
//        {
//            var tracked = _uow.GetRepository<PlayerSideTask>()
//                .FindWithTracking(t => t.SideTaskId == task.SideTaskId);
//            if (tracked is not null)
//            {
//                tracked.Status = SideTaskStatus.Expired;
//                tracked.CompletedAt = DateTime.UtcNow;
//                await _uow.SaveAsync(ct);
//            }
//            return Result.Failure<SideTaskDto>(SideTaskErrors.TaskExpired);
//        }

//        return new SideTaskDto(
//            task.SideTaskId,
//            task.ResolvedTitle,
//            task.ResolvedDescription,
//            task.EgpReward,
//            task.DeadlineAt,
//            task.Status.ToString(),
//            task.ConceptTag);
//    }

//    public async Task<Result<CodeSubmitResponseDto>> SubmitSideTaskAsync(
//        int playerId, SideTaskSubmitRequestDto dto, CancellationToken ct = default)
//    {
//        // 1. Load and guard the task.
//        var task = _uow.GetRepository<PlayerSideTask>()
//            .FindWithTracking(t => t.SideTaskId == dto.SideTaskId && t.PlayerId == playerId);

//        if (task is null)
//            return Result.Failure<CodeSubmitResponseDto>(SideTaskErrors.TaskNotFound);

//        if (task.Status != SideTaskStatus.Active)
//            return Result.Failure<CodeSubmitResponseDto>(SideTaskErrors.TaskAlreadyClosed);

//        if (task.DeadlineAt.HasValue && task.DeadlineAt.Value < DateTime.UtcNow)
//        {
//            task.Status = SideTaskStatus.Expired;
//            task.CompletedAt = DateTime.UtcNow;
//            await _uow.SaveAsync(ct);
//            return Result.Failure<CodeSubmitResponseDto>(SideTaskErrors.TaskExpired);
//        }

//        // 2. Fetch all test cases for the template (visible + hidden).
//        var testCases = await _uow.GetRepository<TestCase>()
//            .FindAll(tc => tc.TaskId == task.TemplateId)
//            .ToListAsync(ct);

//        // 3. Run code execution.
//        var executionResults = await _codeExecutor.ExecuteAsync(dto.SubmittedCode, testCases);

//        // 4. Compute tier from pass rate.
//        int passCount = executionResults.Count(r => r.Passed);
//        double passRate = testCases.Count > 0 ? (double)passCount / testCases.Count : 0;

//        var tier = passRate == 1.0
//            ? ChoiceTier.Ideal
//            : passRate > 0.5
//                ? ChoiceTier.Debt
//                : ChoiceTier.Mistake;

//        // 5. EGP reward multiplier: Ideal=100%, Acceptable=75%, Debt=25%, Mistake=0%.
//        decimal multiplier = tier switch
//        {
//            ChoiceTier.Ideal       => 1.00m,
//            ChoiceTier.Acceptable  => 0.75m,
//            ChoiceTier.Debt        => 0.25m,
//            _                      => 0.00m
//        };
//        decimal egpEarned = Math.Round(task.EgpReward * multiplier, 2);

//        // 6. Persist submission + mark task closed.
//        var submission = new SideTaskSubmission
//        {
//            SideTaskId    = dto.SideTaskId,
//            PlayerId      = playerId,
//            SubmittedCode = dto.SubmittedCode,
//            Tier          = tier,
//            TestResults   = System.Text.Json.JsonSerializer.Serialize(executionResults),
//            SahmHintsUsed = dto.SahmHintsUsed,
//            TimeSpentSec  = dto.TimeSpentSec,
//            EgpEarned     = egpEarned
//        };

//        await _uow.GetRepository<SideTaskSubmission>().AddAsync(submission);
//        task.Status      = SideTaskStatus.Submitted;
//        task.CompletedAt = DateTime.UtcNow;
//        await _uow.SaveAsync(ct);

//        // 7. Credit EGP if earned (outside the save transaction — EconomyService owns its own tx).
//        if (egpEarned > 0)
//        {
//            await _economy.ApplyEgpDeltaAsync(
//                playerId, egpEarned,
//                TransactionType.SideTask,
//                $"Side task reward (task #{dto.SideTaskId})",
//                referenceId: dto.SideTaskId,
//                ct: ct);
//        }

//        var resultDtos = executionResults
//            .Select(r => new TestCaseResultDto
//            {
//                TestCaseId   = r.TestCaseId,
//                Passed       = r.Passed,
//                ActualOutput = r.ActualOutput
//            })
//            .ToList();

//        return new CodeSubmitResponseDto
//        {
//            Tier      = tier.ToString(),
//            TestResults = resultDtos,
//            EgpEarned = egpEarned
//        };
//    }

//    public async Task<Result<AbandonResultDto>> AbandonTaskAsync(
//        int playerId, int sideTaskId, CancellationToken ct = default)
//    {
//        var task = _uow.GetRepository<PlayerSideTask>()
//            .FindWithTracking(t => t.SideTaskId == sideTaskId && t.PlayerId == playerId);

//        if (task is null)
//            return Result.Failure<AbandonResultDto>(SideTaskErrors.TaskNotFound);

//        if (task.Status != SideTaskStatus.Active)
//            return Result.Failure<AbandonResultDto>(SideTaskErrors.TaskAlreadyClosed);

//        // Mark abandoned first, then apply penalty via EconomyService.
//        task.Status      = SideTaskStatus.Abandoned;
//        task.CompletedAt = DateTime.UtcNow;
//        await _uow.SaveAsync(ct);

//        var penaltyResult = await _economy.ApplyEgpDeltaAsync(
//            playerId,
//            -EgpPenalties.Abandonment,
//            TransactionType.Penalty,
//            $"Abandonment penalty (task #{sideTaskId})",
//            referenceId: sideTaskId,
//            ct: ct);

//        if (penaltyResult.IsFailure)
//            return Result.Failure<AbandonResultDto>(penaltyResult.Error);

//        return new AbandonResultDto(
//            PenaltyApplied: -EgpPenalties.Abandonment,
//            NewBalance:     penaltyResult.Value);
//    }

//    public async Task<Result> AssignNewTaskAsync(int playerId, CancellationToken ct = default)
//    {
//        // Guard: player must not already have an active task.
//        var hasActive = await _uow.GetRepository<PlayerSideTask>()
//            .FindAll(t => t.PlayerId == playerId && t.Status == SideTaskStatus.Active)
//            .AnyAsync(ct);

//        if (hasActive)
//            return Result.Failure(SideTaskErrors.AlreadyHasActiveTask);

//        // Get player rank to filter eligible templates.
//        var rank = await _uow.GetRepository<Player>()
//            .FindAll(p => p.PlayerId == playerId)
//            .Select(p => (PlayerRank?)p.Rank)
//            .FirstOrDefaultAsync(ct);

//        if (rank is null)
//            return Result.Failure(EconomyErrors.PlayerNotFound);

//        // Pick a random active template within rank requirement.
//        var templates = await _uow.GetRepository<SideTaskTemplate>()
//            .FindAll(t => t.IsActive && (int)t.RankRequired <= (int)rank.Value)
//            .Select(t => new { t.TemplateId, t.TitleTemplate, t.DescriptionTemplate, t.EgpMin, t.EgpMax })
//            .ToListAsync(ct);

//        if (templates.Count == 0)
//            return Result.Failure(SideTaskErrors.TemplateNotFound);

//        var chosen = templates[Random.Shared.Next(templates.Count)];

//        // Resolve reward within the template's range.
//        var egpRange = (double)(chosen.EgpMax - chosen.EgpMin);
//        var egpReward = chosen.EgpMin + (decimal)(Random.Shared.NextDouble() * egpRange);
//        egpReward = Math.Round(egpReward, 2);

//        var newTask = new PlayerSideTask
//        {
//            PlayerId            = playerId,
//            TemplateId          = chosen.TemplateId,
//            ResolvedTitle       = chosen.TitleTemplate,
//            ResolvedDescription = chosen.DescriptionTemplate,
//            EgpReward           = egpReward,
//            Status              = SideTaskStatus.Active,
//            AssignedAt          = DateTime.UtcNow,
//            DeadlineAt          = DateTime.UtcNow.AddHours(48)
//        };

//        await _uow.GetRepository<PlayerSideTask>().AddAsync(newTask);
//        await _uow.SaveAsync(ct);

//        return Result.Success();
//    }
//}
