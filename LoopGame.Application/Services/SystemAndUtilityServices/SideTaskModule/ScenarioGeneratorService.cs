using LoopGame.Application.Dtos.SideTaskDtos;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Application.IServices.SystemAndUtilityServices;
using LoopGame.Application.Utilities;
using LoopGame.Domain.Entities.Audit;
using LoopGame.Domain.Entities.Code;
using LoopGame.Domain.Entities.SideTask;
using LoopGame.Domain.Enums.AuthModule;
using Microsoft.Extensions.Logging;

namespace LoopGame.Application.Services.SystemAndUtilityServices.SideTaskModule;

public class ScenarioGeneratorService(
    IAssessmentService _assessmentService,
    IFileStorageService _fileStorageService,
    IFileContentReaderService _fileContentReader,
    IAiSideTaskClient _aiClient,
    IUnitOfWork _unitOfWork,
    ILogger<ScenarioGeneratorService> _logger) : IScenarioGeneratorService
{
    public async Task<Result> GenerateForPlayerAsync(int playerId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting AI side task generation pipeline for player {PlayerId}", playerId);

        // 1. Get ALL weak concepts for the player, ordered weakest to strongest
        var weakConceptsResult = await _assessmentService.GetWeakestConceptsAsync(playerId, topN: int.MaxValue, ct: ct);
        if (weakConceptsResult.IsFailure || weakConceptsResult.Value is null || !weakConceptsResult.Value.Any())
        {
            _logger.LogWarning("No weak concepts found for player {PlayerId}", playerId);
            return Result.Failure(SideTaskErrors.NoWeakConcepts);
        }

        var weakSnapshots = weakConceptsResult.Value.ToList();
        _logger.LogInformation("Found {Count} weak concepts for player {PlayerId}", weakSnapshots.Count, playerId);

        // 2. Process each concept independently (weakest -> strongest)
        foreach (var snapshot in weakSnapshots)
        {
            var conceptTag = snapshot.ConceptTag;
            _logger.LogInformation("Processing concept '{Concept}' for player {PlayerId}", conceptTag, playerId);

            // Try to parse string conceptTag into Concept enum
            Enum.TryParse<Concept>(conceptTag, true, out var conceptEnum);

            // Find single SheetFile for this concept
            var sheetFile = await _unitOfWork.GetRepository<SheetFile>()
                .FindAsync(f => f.Concept == conceptEnum || f.Concept.ToString() == conceptTag);

            if (sheetFile is null)
            {
                _logger.LogWarning("No SheetFile found for concept '{Concept}'. Skipping concept.", conceptTag);
                continue;
            }

            if (sheetFile.Status == SheetFileStatus.Failed)
            {
                _logger.LogWarning("SheetFile #{SheetFileId} for concept '{Concept}' is in Failed status. Skipping concept.",
                    sheetFile.Id, conceptTag);
                continue;
            }

            // Download sheet file from S3
            var downloadResult = await _fileStorageService.DownloadAsync(sheetFile.S3Key);
            if (downloadResult.IsFailure)
            {
                _logger.LogWarning("Failed to download S3 key '{S3Key}' for sheet #{SheetFileId}. Skipping concept.",
                    sheetFile.S3Key, sheetFile.Id);
                continue;
            }

            // Read file content
            await using var stream = downloadResult.Value;
            var contentResult = _fileContentReader.ReadFile(stream, sheetFile.FileName);
            if (contentResult.IsFailure)
            {
                _logger.LogWarning("Failed to extract content from sheet #{SheetFileId}: {Error}. Skipping concept.",
                    sheetFile.Id, contentResult.Error.Description);
                continue;
            }
            var sheetContent = contentResult.Value;

            // Find single SideTaskTemplate (Reference Scenario) for this concept
            var template = await _unitOfWork.GetRepository<SideTaskTemplate>()
                            .FindAsync(t => t.ConceptTag == conceptTag && t.IsActive);

            if (template is null)
            {
                _logger.LogWarning("No active SideTaskTemplate found for concept '{Concept}'. Skipping concept.", conceptTag);
                continue;
            }

            // Build request for AI service
            var aiRequest = new AiGenerateRequest
            {
                PlayerId = playerId,
                Concept = conceptTag,
                SheetContent = sheetContent,
                ReferenceScenario = new SideTaskReferenceScenarioRequest
                {
                    TemplateKey = template.TemplateKey,
                    ConceptTag = template.ConceptTag,
                    RankRequired = template.RankRequired,
                    TitleTemplate = template.TitleTemplate,
                    DescriptionTemplate = template.DescriptionTemplate,
                    SlotsSchema = template.SlotsSchema,
                    EgpMin = template.EgpMin,
                    EgpMax = template.EgpMax,
                    CreatedAt = template.CreatedAt
                }
            };

            // AI Generation with Retries (3 attempts total)
            List<AiGeneratedTaskDto> validTasks = [];
            
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                _logger.LogInformation("AI generation attempt {Attempt}/3 for concept '{Concept}', player {PlayerId}",
                    attempt, conceptTag, playerId);

                var aiResult = await _aiClient.GenerateAsync(aiRequest, ct);
                if (aiResult.IsFailure)
                {
                    _logger.LogWarning("AI call failed on attempt {Attempt} for concept '{Concept}': {Error}",
                        attempt, conceptTag, aiResult.Error.Description);
                    continue;
                }

                var returnedTasks = aiResult.Value ?? [];
                
                // Validate each returned task independently
                foreach (var taskDto in returnedTasks)
                {
                    if (IsValidTask(taskDto))
                    {
                        validTasks.Add(taskDto);
                    }
                    else
                    {
                        _logger.LogWarning("Discarded invalid AI task for concept '{Concept}', Title='{Title}'",
                            conceptTag, taskDto.Title);
                    }
                }

                if (validTasks.Count > 0)
                {
                    _logger.LogInformation("AI attempt {Attempt} produced {Count} valid tasks for concept '{Concept}'",
                        attempt, validTasks.Count, conceptTag);
                    break; // Success! Break retry loop
                }

                _logger.LogWarning("AI attempt {Attempt} produced 0 valid tasks for concept '{Concept}'", attempt, conceptTag);
            }

            if (validTasks.Count == 0)
            {
                _logger.LogError("Concept '{Concept}' failed all 3 AI generation attempts. Workflow stopped for this concept.", conceptTag);
                continue;
            }

            // Sort valid tasks easiest to hardest by Difficulty
            var orderedTasks = validTasks.OrderBy(t => t.Difficulty).ToList();

            // Save each valid task as PlayerSideTask + TestCase rows
            foreach (var taskDto in orderedTasks)
            {
                var aiLog = new AiGenerationLog
                {
                    PlayerId = playerId,
                    TemplateId = template.TemplateId,
                    ModelName = "External-AI-Service",
                    PromptText = $"Concept: {conceptTag}",
                    ParsedSlots = taskDto.FilledSlotsJson,
                    IsValid = true,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddYears(2)
                };
                await _unitOfWork.GetRepository<AiGenerationLog>().AddAsync(aiLog);
                await _unitOfWork.SaveAsync(ct);

                var playerTask = new PlayerSideTask
                {
                    PlayerId = playerId,
                    TemplateId = template.TemplateId,
                    AiLogId = aiLog.LogId,
                    ResolvedTitle = taskDto.Title,
                    ResolvedDescription = taskDto.Description,
                    FilledSlots = taskDto.FilledSlotsJson,
                    EgpReward = taskDto.EgpReward > 0 ? taskDto.EgpReward : template.EgpMin,
                    Status = SideTaskStatus.Active,
                    AssignedAt = DateTime.UtcNow
                };
                await _unitOfWork.GetRepository<PlayerSideTask>().AddAsync(playerTask);
                await _unitOfWork.SaveAsync(ct);

                foreach (var tcDto in taskDto.TestCases)
                {
                    var testCase = new TestCase
                    {
                        SideTaskId = playerTask.SideTaskId,
                        TestInput = tcDto.TestInput,
                        ExpectedOutput = tcDto.ExpectedOutput,
                        IsHidden = tcDto.IsHidden,
                        Description = tcDto.Description
                    };
                    await _unitOfWork.GetRepository<TestCase>().AddAsync(testCase);
                }
                await _unitOfWork.SaveAsync(ct);
            }

            _logger.LogInformation("Successfully saved {Count} side tasks for concept '{Concept}' for player {PlayerId}",
                orderedTasks.Count, conceptTag, playerId);
        }

        return Result.Success();
    }

    private static bool IsValidTask(AiGeneratedTaskDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return false;
        if (string.IsNullOrWhiteSpace(dto.Description)) return false;
        if (dto.TestCases == null || dto.TestCases.Count == 0) return false;
        foreach (var tc in dto.TestCases)
        {
            if (tc == null || tc.ExpectedOutput == null) return false;
        }
        return true;
    }
}
