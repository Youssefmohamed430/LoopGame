using LoopGame.Application.Dtos.NarrativeDtos;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Domain.Abstractions;
using LoopGame.Domain.Entities.Narrative;
using LoopGame.Domain.Entities.Player;
using LoopGame.Domain.Enums;
using Mapster;

namespace LoopGame.Application.Services.LearningAndContentServices;

public class NarrativeService(IUnitOfWork unitOfWork) : INarrativeService
{
    public async Task<Result<NarrativeFlowDto>> StartShift(int playerId, int shiftId)
    {
        // 1. Validate Player and Shift Access
        var (player, failure) = await ValidatePlayerAccess(playerId, shiftId);
        if (failure != null)
            return failure;
        
        // 2. Get Shift
        var (shift, failure1) = await GetShift(shiftId);
        if (failure1 != null)
            return failure1;
        
        // 3. Fetch standard narrative beats for this shift (ordered by sequence_order)
        var beatsDto = GetBeats(shiftId);

        // 4. Fetch pending consequence beats targeted for this shift
        var pendingConsequences = GetPendingConsequences(playerId, shiftId);

        // 5. Categorize consequences by InjectPosition ('start' vs 'end') and mark them as fired
        var (startConsequenceBeats, endConsequenceBeats) = await CategorizeConsequencesByInjectPosition(pendingConsequences);

        // 6. Merge Narrative Flow: [Start Consequences] -> [Standard Narrative Beats] -> [End Consequences]
        var mergedBeats = MergedBeats(startConsequenceBeats, beatsDto, endConsequenceBeats);

        // 7. Save changes & construct response DTO using Mapster
        await unitOfWork.SaveAsync();

        var narrativeFlowDto = new NarrativeFlowDto
        {
            ShiftId = shift.ShiftId,
            Shift = shift.Adapt<ShiftDto>(),
            Beats = mergedBeats
        };

        return Result.Success(narrativeFlowDto);
    }

    private async Task<(Shift? shift, Result<NarrativeFlowDto> result)> GetShift(int shiftId)
    {
        var shift = await unitOfWork.GetRepository<Shift>()
            .FindAsync(s => s.ShiftId == shiftId);

        if (shift == null)
            return (shift, Result.Failure<NarrativeFlowDto>(ChoiceErrors.ShiftMismatch));
        return (shift, null!);
    }

    private List<BeatDto> GetBeats(int shiftId)
    {
        var narrativeBeats = unitOfWork.GetRepository<StoryBeat>()
            .FindAll(sb => sb.ShiftId == shiftId && sb.BeatType == BeatType.Narrative, ["Choices"])
            .OrderBy(sb => sb.SequenceOrder)
            .ToList();

        var beatsDto = narrativeBeats.Adapt<List<BeatDto>>();
        return beatsDto;
    }

    private List<ConsequenceQueue> GetPendingConsequences(int playerId, int shiftId)
    {
        var pendingConsequences = unitOfWork.GetRepository<ConsequenceQueue>()
            .FindAll(cq => cq.PlayerId == playerId &&
                           cq.Status == ConsequenceStatus.pending &&
                           cq.Consequence.Beat.ShiftId == shiftId,
                ["Consequence", "Consequence.Beat", "Consequence.Beat.Choices"])
            .ToList();
        return pendingConsequences;
    }

    private static List<BeatDto> MergedBeats(List<BeatDto> startConsequenceBeats, List<BeatDto> beatsDto, List<BeatDto> endConsequenceBeats)
    {
        var mergedBeats = new List<BeatDto>();
        mergedBeats.AddRange(startConsequenceBeats);
        mergedBeats.AddRange(beatsDto);
        mergedBeats.AddRange(endConsequenceBeats);
        return mergedBeats;
    }

    private async Task<(List<BeatDto> startConsequenceBeats, List<BeatDto> endConsequenceBeats)> CategorizeConsequencesByInjectPosition(List<ConsequenceQueue> pendingConsequences)
    {
        var startConsequenceBeats = new List<BeatDto>();
        var endConsequenceBeats = new List<BeatDto>();

        foreach (var cq in pendingConsequences)
        {
            if (cq.Consequence?.Beat != null)
            {
                var consequenceBeatDto = cq.Consequence.Beat.Adapt<BeatDto>();
                if (string.Equals(cq.Consequence.InjectPosition, "end", StringComparison.OrdinalIgnoreCase))
                {
                    endConsequenceBeats.Add(consequenceBeatDto);
                }
                else
                {
                    startConsequenceBeats.Add(consequenceBeatDto);
                }
            }

            cq.Status = ConsequenceStatus.fired;
            cq.FiredAt = DateTime.UtcNow;
            await unitOfWork.GetRepository<ConsequenceQueue>().UpdateAsync(cq);
        }

        return (startConsequenceBeats, endConsequenceBeats);
    }

    private async Task<(Player?, Result<NarrativeFlowDto>?)> ValidatePlayerAccess(int playerId, int shiftId)
    {
        var player = await unitOfWork.GetRepository<Player>()
            .FindAsync(p => p.PlayerId == playerId);

        if (player == null)
            return (null, Result.Failure<NarrativeFlowDto>(ChoiceErrors.PlayerNotFound));

        if (player.CurrentShiftId != shiftId)
            return (null, Result.Failure<NarrativeFlowDto>(ChoiceErrors.ShiftMismatch));

        return (player, null);
    }
}