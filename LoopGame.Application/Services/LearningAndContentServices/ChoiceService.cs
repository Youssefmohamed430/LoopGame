using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Domain.Abstractions;
using LoopGame.Domain.Constants;
using LoopGame.Domain.Entities.Narrative;
using LoopGame.Domain.Entities.Player;
using Mapster;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LoopGame.Application.Services.LearningAndContentServices;

public class ChoiceService
    (IUnitOfWork unitOfWork, IEventPublisher eventPublisher)
    : IChoiceService
{
    public async Task<Result<List<ChoiceDto>>> GetChoices(int beatId, int playerId)
    {
        if (beatId <= 0 || playerId <= 0)
            return Result.Failure<List<ChoiceDto>>(ChoiceErrors.InvalidId);

        var player = await unitOfWork.GetRepository<Player>()
            .FindAsync(p => p.PlayerId == playerId);

        if (player == null)
            return Result.Failure<List<ChoiceDto>>(ChoiceErrors.PlayerNotFound);

        var beat = await unitOfWork.GetRepository<StoryBeat>()
            .FindAsync(b => b.BeatId == beatId, new string[] { "Choices" });

        if (beat == null)
            return Result.Failure<List<ChoiceDto>>(ChoiceErrors.BeatNotFound);

        if (player.CurrentShiftId != beat.ShiftId)
            return Result.Failure<List<ChoiceDto>>(ChoiceErrors.ShiftMismatch);

        var listOfChoices = beat.Choices
            .Select(c => c.Adapt<ChoiceDto>())
            .ToList();

        return Result.Success(listOfChoices);
    }

    public async Task<Result<List<ChoiceDto>>> AddChoice(List<CreateChoiceDto> choices)
    {
        if (choices == null || choices.Count == 0)
            return Result.Failure<List<ChoiceDto>>(ChoiceErrors.EmptyChoicesList);

        if (choices.Count > 4)
            return Result.Failure<List<ChoiceDto>>(ChoiceErrors.ExceedsMaxChoices);

        if (ValidateChoices(choices, out var result)) return result;

        if (CheckDuplicate(choices, out var failure)) return failure;

        var (flag, failure1) = await ValidateBeatsOfChoices(choices);
        if (!flag) return failure1;

        (flag,failure1) =  await ValidateConsequences(choices);
        if (!flag) return failure1;

        var addedEntities = new List<Choice>();
        foreach (var choiceDto in choices)
        {
            var entity = choiceDto.Adapt<Choice>();
            await unitOfWork.GetRepository<Choice>().AddAsync(entity);
            addedEntities.Add(entity);
        }

        await unitOfWork.SaveAsync();

        return Result.Success(addedEntities.Adapt<List<ChoiceDto>>());
    }

    private async Task<(bool,Result<List<ChoiceDto>>)> ValidateConsequences(List<CreateChoiceDto> choices)
    {
        foreach (var choice in choices)
        {
            if (choice.ConsequenceId.HasValue)
            {
                var consequence = await unitOfWork.GetRepository<Consequence>()
                    .FindAsync(c => c.ConsequenceId == choice.ConsequenceId.Value);

                if (consequence == null)
                    return (false,Result.Failure<List<ChoiceDto>>(ChoiceErrors.InvalidConsequence));
            }
        }

        return (true,null!);
    }

    private async Task<(bool,Result<List<ChoiceDto>>)> ValidateBeatsOfChoices(List<CreateChoiceDto> choices)
    {
        var beatIds = choices.Select(c => c.BeatId).Distinct();
        foreach (var beatId in beatIds)
        {
            var beat = await unitOfWork.GetRepository<StoryBeat>()
                .FindAsync(b => b.BeatId == beatId, new string[] { "Choices" });

            if (beat == null)
                return (false,Result.Failure<List<ChoiceDto>>(ChoiceErrors.BeatNotFound));

            int existingCount = beat.Choices?.Count ?? 0;
            int newCount = choices.Count(c => c.BeatId == beatId);

            if (existingCount + newCount > 4)
                return (false,Result.Failure<List<ChoiceDto>>(ChoiceErrors.ExceedsMaxChoices));

            if (beat.Choices != null)
            {
                var newIndexesForBeat = choices.Where(c => c.BeatId == beatId).Select(c => c.ChoiceIndex);
                if (beat.Choices.Any(existing => newIndexesForBeat.Contains(existing.ChoiceIndex)))
                    return (false,Result.Failure<List<ChoiceDto>>(ChoiceErrors.DuplicateChoiceIndex));
            }
        }

        return (true,null!);
    }

    private static bool ValidateChoices(List<CreateChoiceDto> choices, out Result<List<ChoiceDto>> result)
    {
        foreach (var choice in choices)
        {
            if (choice.BeatId <= 0)
            {
                result = Result.Failure<List<ChoiceDto>>(ChoiceErrors.InvalidId);
                return true;
            }

            if (choice.ChoiceIndex < 1 || choice.ChoiceIndex > 4)
            {
                result = Result.Failure<List<ChoiceDto>>(ChoiceErrors.InvalidChoiceIndex);
                return true;
            }

            if (string.IsNullOrWhiteSpace(choice.ChoiceText))
            {
                result = Result.Failure<List<ChoiceDto>>(ChoiceErrors.InvalidChoiceText);
                return true;
            }
        }

        result = null!;
        return false;
    }

    private static bool CheckDuplicate(List<CreateChoiceDto> choices, out Result<List<ChoiceDto>> failure)
    {
        bool intraDuplicate = choices
            .GroupBy(c => new { c.BeatId, c.ChoiceIndex })
            .Any(g => g.Count() > 1);

        if (intraDuplicate)
        {
            failure = Result.Failure<List<ChoiceDto>>(ChoiceErrors.DuplicateChoiceIndex);
            return true;
        }

        failure = null!;
        return false;
    }

    public async Task<Result<ChoiceDto>> SubmitChoice(int choiceid, int PlayerId)
    {
        var player = await unitOfWork.GetRepository<Player>()
            .FindAsync(p => p.PlayerId == PlayerId,new string [] {"ShiftProgresses"});

        var choice = await unitOfWork.GetRepository<Choice>()
            .FindAsync(c => c.ChoiceId == choiceid,new string[] {"Beat"});

        if (choice.Beat.ShiftId != player.CurrentShiftId)
            return Result.Failure<ChoiceDto>(new Error("Forbidden.Access","You are not allowed to access this choice."));

        var PlayerChoice = new PlayerChoice()
        {
            PlayerId = PlayerId,
            ChoiceId = choiceid,
            BeatId = choice.BeatId,
            Tier = choice.Tier
        };
        await unitOfWork.GetRepository<PlayerChoice>()
            .AddAsync(PlayerChoice);

        if (choice.ConsequenceId != null)
        {
            var ConsequenceQueue = new ConsequenceQueue()
            {
                PlayerId = PlayerId,
                ConsequenceId = Convert.ToInt32(choice.ConsequenceId)
            };
            await unitOfWork.GetRepository<ConsequenceQueue>()
                .AddAsync(ConsequenceQueue);
        }
        var playerProgress = player.ShiftProgresses.FirstOrDefault(p => p.ShiftId == player.CurrentShiftId)!;
        playerProgress.GateAttempts++;
        await unitOfWork.SaveAsync();

        // ── Event telemetry (fire-and-forget, after persistence) ──
        eventPublisher.Publish(new GameEventDto(
            PlayerId,
            EventType:   AssessmentWeights.EventTypes.ChoiceSubmission,
            ConceptTag:  choice.Beat.BeatKey,
            Tier:        choice.Tier.ToString(),
            PayloadJson: JsonSerializer.Serialize(new
            {
                beatId   = choice.BeatId,
                choiceId = choice.ChoiceId
            })));

        return Result.Success(choice.Adapt<ChoiceDto>());
    }

    public async Task<Result<ChoiceDto>> UpdateChoice(int choiceid, UpdateChoiceDto choicedto)
    {
        if (choiceid <= 0)
            return Result.Failure<ChoiceDto>(ChoiceErrors.InvalidId);

        if (choicedto == null)
            return Result.Failure<ChoiceDto>(ChoiceErrors.ChoiceNotFound);

        var choice = unitOfWork.GetRepository<Choice>()
            .FindWithTracking(c => c.ChoiceId == choiceid);

        if (choice == null)
            return Result.Failure<ChoiceDto>(ChoiceErrors.ChoiceNotFound);

        if (choicedto.ChoiceText != null && string.IsNullOrWhiteSpace(choicedto.ChoiceText))
            return Result.Failure<ChoiceDto>(ChoiceErrors.InvalidChoiceText);

        if (choicedto.ConsequenceId.HasValue && choicedto.ConsequenceId != choice.ConsequenceId)
        {
            var consequence = await unitOfWork.GetRepository<Consequence>()
                .FindAsync(c => c.ConsequenceId == choicedto.ConsequenceId.Value);

            if (consequence == null)
                return Result.Failure<ChoiceDto>(ChoiceErrors.InvalidConsequence);
        }

        choice.ChoiceText = choicedto.ChoiceText ?? choice.ChoiceText;
        choice.ConsequenceId = choicedto.ConsequenceId ?? choice.ConsequenceId;
        choice.ImmediateFeedback = choicedto.ImmediateFeedback ?? choice.ImmediateFeedback;

        await unitOfWork.GetRepository<Choice>()
            .UpdateAsync(choice);

        await unitOfWork.SaveAsync();

        return Result.Success(choice.Adapt<ChoiceDto>());
    }
}
