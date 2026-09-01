using System.Text.Json;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Domain.Constants;
using LoopGame.Domain.Entities.Player;

namespace LoopGame.Application.Services.LearningAndContentServices;

/// <summary>
/// Application-layer orchestrator for the Practice / Gate submission use case.
///
/// Responsibility: coordinate the SubmitCode workflow without owning any
/// individual business rule. All detailed logic is delegated to focused
/// specialist components.
///
/// Also handles task/test-case CRUD (Admin operations). If the project grows,
/// those can be split into a dedicated PracticeTaskAdminService.
/// </summary>
public class PracticeService(
    IUnitOfWork                _uow,
    IPracticeAccessService     _accessService,
    IAttemptPolicy             _attemptPolicy,
    ICodeExecutionService      _codeExecutor,
    ITierCalculationPolicy     _tierPolicy,
    IPracticeAttemptService    _attemptService,
    IProgressionService        _progressionService,
    IAssessmentEventEmitter    _assessmentEmitter,
    IAssessmentJobScheduler    _assessmentJobScheduler)
    : IPracticeService
{
    // ══════════════════════════════════════════════════════════════════════════
    // Player-facing: get task (visible test cases only)
    // ══════════════════════════════════════════════════════════════════════════

    public async Task<Result<PracticeDto>> GetTaskAsync(int TaskId, int PlayerId)
    {
        var task = _uow.GetRepository<PracticeTask>()
            .Find<PracticeDto>(pt => pt.TaskId == TaskId, new[] { "TestCases", "Shift" });

        if (task is null)
            return Result.Failure<PracticeDto>(PracticeErrors.TaskNotFound);

        var accessResult = await _accessService.ValidateAccessAsync(PlayerId, TaskId);
        if (accessResult.IsFailure)
            return Result.Failure<PracticeDto>(accessResult.Error);

        // Filter out hidden test cases for the player-facing view.
        task.TestCases = task.TestCases?.Where(t => !t.IsHidden).ToList();

        return Result.Success(task);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Player-facing: submit code (main use-case orchestration)
    // ══════════════════════════════════════════════════════════════════════════

    public async Task<Result<CodeSubmitResponseDto>> SubmitCode(int PlayerId, CodeSubmitRequestDto code)
    {
        // ── 1. Validate access ─────────────────────────────────────────────────
        var accessResult = await _accessService.ValidateAccessAsync(PlayerId, code.TaskId);
        if (accessResult.IsFailure)
            return Result.Failure<CodeSubmitResponseDto>(accessResult.Error);

        var ctx = accessResult.Value;

        // ── 2. Load task with ALL test cases (including hidden) ────────────────
        var task = _uow.GetRepository<PracticeTask>()
            .Find(t => t.TaskId == code.TaskId, new[] { "TestCases", "Shift" });

        if (task is null)
            return Result.Failure<CodeSubmitResponseDto>(PracticeErrors.TaskNotFound);

        // ── 3. Validate MaxAttempts ────────────────────────────────────────────
        var attemptCheck = _attemptPolicy.CheckCanAttempt(PlayerId, code.TaskId, task.MaxAttempts);
        if (attemptCheck.IsFailure)
            return Result.Failure<CodeSubmitResponseDto>(attemptCheck.Error);

        // ── 4. Execute submitted code against all test cases ───────────────────
        var testResults = (await _codeExecutor.ExecuteAsync(code.SubmittedCode, task.TestCases.ToList()))
            .AsReadOnly();

        // ── 5. Calculate tier ──────────────────────────────────────────────────
        var tier = _tierPolicy.Calculate(testResults);

        // ── 6. Record PracticeAttempt (staged, not yet committed) ──────────────
        var attemptId = await _attemptService.RecordAttemptAsync(
            PlayerId, code.TaskId, code.SubmittedCode, tier,
            testResults, code.HintUsed, code.TimeSpentSec);

        // ── 7. Process PlayerShiftProgress / Gate (staged, not yet committed) ──
        var progressResult = await _progressionService.ProcessSubmissionAsync(ctx.ShiftProgress, tier);
        if (progressResult.IsFailure)
            return Result.Failure<CodeSubmitResponseDto>(progressResult.Error);

        var gateProgress = progressResult.Value;

        // ── 8. Commit all staged changes atomically ────────────────────────────
        await _uow.SaveAsync();

        // ── 9. Emit PracticeAttempt assessment event (fire-and-forget) ─────────
        _assessmentEmitter.Emit(new AssessmentEventDto(
            PlayerId,
            EventType:   AssessmentWeights.EventTypes.PracticeAttempt,
            ConceptTag:  task.ConceptTag,
            Tier:        tier.ToString(),
            PayloadJson: JsonSerializer.Serialize(new
            {
                taskId       = code.TaskId,
                attemptId,
                timeSpentSec = code.TimeSpentSec,
                testResults
            })));

        // ── 10. If gate was cleared: emit telemetry + schedule mastery ─────────
        if (gateProgress.GateCleared)
        {
            _assessmentEmitter.Emit(new AssessmentEventDto(
                PlayerId,
                EventType:   AssessmentWeights.EventTypes.GateCleared,
                ConceptTag:  null,
                Tier:        null,
                PayloadJson: JsonSerializer.Serialize(new { shiftId = ctx.ShiftId, taskId = code.TaskId })));

            _assessmentEmitter.Emit(new AssessmentEventDto(
                PlayerId,
                EventType:   AssessmentWeights.EventTypes.ShiftCompleted,
                ConceptTag:  null,
                Tier:        null,
                PayloadJson: JsonSerializer.Serialize(new { shiftId = ctx.ShiftId })));

            _assessmentJobScheduler.EnqueueMasteryComputation(PlayerId, ctx.ShiftId);
        }

        // ── 11. Build and return response ──────────────────────────────────────
        bool struggleDetected = ctx.Player.PracticeAttempts
            .Count(p => p.TaskId == code.TaskId) > 4;

        return Result.Success(new CodeSubmitResponseDto
        {
            Tier             = tier,
            TestResults      = JsonSerializer.Serialize(testResults),
            GateCleared      = gateProgress.GateCleared,
            StruggleDetected = struggleDetected,
        });
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Admin / Task management operations
    // ══════════════════════════════════════════════════════════════════════════

    public Result<PracticeDto> AddPracticeTask(PracticeDto practice)
    {
        var task = practice.Adapt<PracticeTask>();
        _uow.GetRepository<PracticeTask>().AddAsync(task);
        _uow.SaveAsync().GetAwaiter().GetResult();
        return Result.Success(practice);
    }

    public Result<PracticeDto> UpdatePracticeTask(int TaskId, PracticeDto practice)
    {
        var task = _uow.GetRepository<PracticeTask>()
            .FindWithTracking(pt => pt.TaskId == TaskId, new[] { "TestCases", "Shift", "Attempts" });

        if (task is null)
            return Result.Failure<PracticeDto>(PracticeErrors.TaskNotFound);

        ApplyTaskUpdates(practice, task);
        _uow.GetRepository<PracticeTask>().UpdateAsync(task);
        _uow.SaveAsync().GetAwaiter().GetResult();
        return Result.Success(practice);
    }

    public Result<TestCaseDto> UpdateTestCasesAtPracticeTask(int TestId, TestCaseDto testCaseDto)
    {
        var testCase = _uow.GetRepository<TestCase>()
            .FindWithTracking(tc => tc.TestCaseId == TestId);

        if (testCase is null)
            return Result.Failure<TestCaseDto>(new Error("NotFound.TestCase", "TestCase was not found."));

        testCase.TestInput      = testCaseDto.TestInput      ?? testCase.TestInput;
        testCase.Description    = testCaseDto.Description    ?? testCase.Description;
        testCase.ExpectedOutput = testCaseDto.ExpectedOutput ?? testCase.ExpectedOutput;
        testCase.IsHidden       = testCaseDto.IsHidden;

        _uow.GetRepository<TestCase>().UpdateAsync(testCase);
        _uow.SaveAsync().GetAwaiter().GetResult();
        return Result.Success(testCaseDto);
    }

    public Result<List<TestCaseDto>> AddTestCasesAtPracticeTask(List<TestCaseDto> testCaseDtos)
    {
        foreach (var testCase in testCaseDtos)
        {
            var entity = testCase.Adapt<TestCase>();
            _uow.GetRepository<TestCase>().AddAsync(entity);
        }
        _uow.SaveAsync().GetAwaiter().GetResult();
        return Result.Success(testCaseDtos);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Private helpers
    // ══════════════════════════════════════════════════════════════════════════

    private static void ApplyTaskUpdates(PracticeDto practice, PracticeTask task)
    {
        task.MaxAttempts = practice.MaxAttempts ?? task.MaxAttempts;
        task.StarterCode = practice.StarterCode ?? task.StarterCode;
        task.ConceptTag  = !string.IsNullOrWhiteSpace(practice.ConceptTag)  ? practice.ConceptTag  : task.ConceptTag;
        task.Description = !string.IsNullOrWhiteSpace(practice.Description) ? practice.Description : task.Description;
        task.Difficulty  = !string.IsNullOrWhiteSpace(practice.Difficulty)  ? practice.Difficulty  : task.Difficulty;
        task.TaskOrder   = practice.TaskOrder ?? task.TaskOrder;
        task.Title       = !string.IsNullOrWhiteSpace(practice.Title) ? practice.Title : task.Title;
        task.EgpReward   = practice.EgpReward ?? task.EgpReward;
    }
}
