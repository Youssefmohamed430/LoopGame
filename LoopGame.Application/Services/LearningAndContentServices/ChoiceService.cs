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

        var beat = unitOfWork.GetRepository<StoryBeat>()
            .FindAll(b => b.BeatId == beatId, ["Choices"])
            .FirstOrDefault();

        if (beat == null)
            return Result.Failure<List<ChoiceDto>>(ChoiceErrors.BeatNotFound);

        if (beat.PlayerId.HasValue && beat.PlayerId.Value != playerId)
            return Result.Failure<List<ChoiceDto>>(ChoiceErrors.ForbiddenAccess);

        if (!player.CurrentShiftId.HasValue || player.CurrentShiftId.Value != beat.ShiftId)
            return Result.Failure<List<ChoiceDto>>(ChoiceErrors.ShiftMismatch);

        var listOfChoices = beat.Choices
            .OrderBy(c => c.ChoiceIndex)
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

    private Task<(bool, Result<List<ChoiceDto>>)> ValidateConsequences(List<CreateChoiceDto> choices)
    {
        foreach (var choice in choices)
        {
            if (choice.ConsequenceId.HasValue)
            {
                var consequence = unitOfWork.GetRepository<Consequence>()
                    .FindAll(c => c.ConsequenceId == choice.ConsequenceId.Value, ["Beat"])
                    .FirstOrDefault();

                if (consequence == null)
                    return Task.FromResult<(bool, Result<List<ChoiceDto>>)>(
                        (false, Result.Failure<List<ChoiceDto>>(ChoiceErrors.InvalidConsequence)));

                if (consequence.Beat != null && (consequence.Beat.IsAiGenerated || consequence.Beat.PlayerId != null))
                    return Task.FromResult<(bool, Result<List<ChoiceDto>>)>(
                        (false, Result.Failure<List<ChoiceDto>>(ChoiceErrors.DynamicBeatForbidden)));
            }
        }

        return Task.FromResult<(bool, Result<List<ChoiceDto>>)>((true, null!));
    }

    private Task<(bool, Result<List<ChoiceDto>>)> ValidateBeatsOfChoices(List<CreateChoiceDto> choices)
    {
        var beatIds = choices.Select(c => c.BeatId).Distinct();
        foreach (var beatId in beatIds)
        {
            var beat = unitOfWork.GetRepository<StoryBeat>()
                .FindAll(b => b.BeatId == beatId, ["Choices"])
                .FirstOrDefault();

            if (beat == null)
                return Task.FromResult<(bool, Result<List<ChoiceDto>>)>(
                    (false, Result.Failure<List<ChoiceDto>>(ChoiceErrors.BeatNotFound)));

            if (beat.IsAiGenerated || beat.PlayerId != null)
                return Task.FromResult<(bool, Result<List<ChoiceDto>>)>(
                    (false, Result.Failure<List<ChoiceDto>>(ChoiceErrors.DynamicBeatForbidden)));

            if (!beat.HasChoices)
                return Task.FromResult<(bool, Result<List<ChoiceDto>>)>(
                    (false, Result.Failure<List<ChoiceDto>>(ChoiceErrors.NotAllowedToAddChoice)));

            int existingCount = beat.Choices?.Count ?? 0;
            int newCount = choices.Count(c => c.BeatId == beatId);

            if (existingCount + newCount > 4)
                return Task.FromResult<(bool, Result<List<ChoiceDto>>)>(
                    (false, Result.Failure<List<ChoiceDto>>(ChoiceErrors.ExceedsMaxChoices)));

            if (beat.Choices != null)
            {
                var newIndexesForBeat = choices.Where(c => c.BeatId == beatId).Select(c => c.ChoiceIndex);
                if (beat.Choices.Any(existing => newIndexesForBeat.Contains(existing.ChoiceIndex)))
                    return Task.FromResult<(bool, Result<List<ChoiceDto>>)>(
                        (false, Result.Failure<List<ChoiceDto>>(ChoiceErrors.DuplicateChoiceIndex)));
            }
        }

        return Task.FromResult<(bool, Result<List<ChoiceDto>>)>((true, null!));
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
        if (choiceid <= 0 || PlayerId <= 0)
            return Result.Failure<ChoiceDto>(ChoiceErrors.InvalidId);

        var player = await unitOfWork.GetRepository<Player>()
            .FindAsync(p => p.PlayerId == PlayerId);

        if (player == null)
            return Result.Failure<ChoiceDto>(ChoiceErrors.PlayerNotFound);

        if (!player.CurrentShiftId.HasValue || player.CurrentShiftId.Value <= 0)
            return Result.Failure<ChoiceDto>(ChoiceErrors.ShiftMismatch);

        var choice = unitOfWork.GetRepository<Choice>()
            .FindAll(
                c => c.ChoiceId == choiceid,
                ["Beat", "Beat.Shift", "Consequence", "Consequence.Beat"])
            .FirstOrDefault();

        if (choice == null)
            return Result.Failure<ChoiceDto>(ChoiceErrors.ChoiceNotFound);

        if (choice.Beat == null)
            return Result.Failure<ChoiceDto>(ChoiceErrors.BeatNotFound);

        if (choice.Beat.PlayerId.HasValue && choice.Beat.PlayerId.Value != PlayerId)
            return Result.Failure<ChoiceDto>(ChoiceErrors.ForbiddenAccess);

        if (choice.Beat.ShiftId != player.CurrentShiftId.Value)
            return Result.Failure<ChoiceDto>(ChoiceErrors.ShiftMismatch);

        bool alreadySubmitted = unitOfWork.GetRepository<PlayerChoice>()
            .FindAll(pc => pc.PlayerId == PlayerId && pc.BeatId == choice.BeatId)
            .Any();

        if (alreadySubmitted)
            return Result.Failure<ChoiceDto>(ChoiceErrors.DuplicateChoiceSubmission);

        // Invariant 2: choices belonging to AI-generated beats never count toward assessment or gate progression.
        bool isEvaluateable = choice.IsEvaluateable && !choice.Beat.IsAiGenerated;

        PlayerShiftProgress? playerProgress = null;
        if (isEvaluateable)
        {
            playerProgress = await unitOfWork.GetRepository<PlayerShiftProgress>()
                .FindAsync(p => p.PlayerId == PlayerId && p.ShiftId == player.CurrentShiftId.Value);

            if (playerProgress == null)
                return Result.Failure<ChoiceDto>(ChoiceErrors.ShiftProgressNotFound);
        }

        if (choice.ConsequenceId.HasValue)
        {
            var consequenceBeat = choice.Consequence?.Beat;
            if (consequenceBeat != null &&
                consequenceBeat.PlayerId.HasValue &&
                consequenceBeat.PlayerId.Value != PlayerId)
            {
                return Result.Failure<ChoiceDto>(ChoiceErrors.ForbiddenAccess);
            }

            int consequenceId = choice.ConsequenceId.Value;
            bool alreadyQueued = unitOfWork.GetRepository<ConsequenceQueue>()
                .FindAll(cq => cq.PlayerId == PlayerId && cq.ConsequenceId == consequenceId)
                .Any();

            if (!alreadyQueued)
            {
                await unitOfWork.GetRepository<ConsequenceQueue>().AddAsync(new ConsequenceQueue
                {
                    PlayerId      = PlayerId,
                    ConsequenceId = consequenceId
                });
            }
        }

        await unitOfWork.GetRepository<PlayerChoice>().AddAsync(new PlayerChoice
        {
            PlayerId = PlayerId,
            ChoiceId = choice.ChoiceId,
            BeatId   = choice.BeatId,
            Tier     = choice.Tier
        });

        if (isEvaluateable && playerProgress != null)
            playerProgress.GateAttempts++;

        await unitOfWork.SaveAsync();

        // ── Event telemetry (fire-and-forget, after persistence) ──
        if (isEvaluateable)
        {
            eventPublisher.Publish(new GameEventDto(
                PlayerId,
                EventType:   AssessmentWeights.EventTypes.ChoiceSubmission,
                ConceptTag:  choice.Beat.Shift?.ConceptTag ?? default,
                Tier:        choice.Tier.ToString(),
                PayloadJson: JsonSerializer.Serialize(new
                {
                    beatId   = choice.BeatId,
                    choiceId = choice.ChoiceId
                })));
        }

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

        var parentBeat = unitOfWork.GetRepository<StoryBeat>()
            .FindAll(b => b.BeatId == choice.BeatId)
            .Select(b => new { b.IsAiGenerated, b.PlayerId })
            .FirstOrDefault();

        if (parentBeat != null && (parentBeat.IsAiGenerated || parentBeat.PlayerId != null))
            return Result.Failure<ChoiceDto>(ChoiceErrors.DynamicBeatForbidden);

        if (choicedto.ChoiceText != null && string.IsNullOrWhiteSpace(choicedto.ChoiceText))
            return Result.Failure<ChoiceDto>(ChoiceErrors.InvalidChoiceText);

        if (choicedto.ConsequenceId.HasValue && choicedto.ConsequenceId != choice.ConsequenceId)
        {
            var consequence = unitOfWork.GetRepository<Consequence>()
                .FindAll(c => c.ConsequenceId == choicedto.ConsequenceId.Value, ["Beat"])
                .FirstOrDefault();

            if (consequence == null)
                return Result.Failure<ChoiceDto>(ChoiceErrors.InvalidConsequence);

            if (consequence.Beat != null && (consequence.Beat.IsAiGenerated || consequence.Beat.PlayerId != null))
                return Result.Failure<ChoiceDto>(ChoiceErrors.DynamicBeatForbidden);
        }

        choice.ChoiceText = choicedto.ChoiceText ?? choice.ChoiceText;
        choice.ConsequenceId = choicedto.ConsequenceId ?? choice.ConsequenceId;
        choice.ImmediateFeedback = choicedto.ImmediateFeedback ?? choice.ImmediateFeedback;
        choice.Tier = choicedto.tier ?? choice.Tier;

        await unitOfWork.GetRepository<Choice>()
            .UpdateAsync(choice);

        await unitOfWork.SaveAsync();

        return Result.Success(choice.Adapt<ChoiceDto>());
    }
}
