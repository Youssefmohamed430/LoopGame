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
    // ═══════════════════════════════════════════════════════════════════════
    // Runtime
    // ═══════════════════════════════════════════════════════════════════════

    public async Task<Result<NarrativeFlowDto>> StartShift(int playerId, int shiftId)
    {
        // 1. Validate Player and Shift Access
        var (player, failure) = await ValidatePlayerAccess(playerId, shiftId);
        if (failure != null)
            return failure;

        // 2. Get Shift
        var (shift, failure1) = await GetShiftEntityForRuntime(shiftId);
        if (failure1 != null)
            return failure1;

        // 3. Fetch standard narrative beats for this shift (ordered by sequence_order)
        var beatsDto = GetNarrativeBeats(shiftId);

        // 4. Fetch pending consequence beats targeted for this shift
        var pendingConsequences = GetPendingConsequences(playerId, shiftId);

        // 5. Categorize consequences by InjectPosition ('start' vs 'end') and mark them as fired
        var (startConsequenceBeats, endConsequenceBeats) =
            await CategorizeConsequencesByInjectPosition(pendingConsequences);

        // 6. Merge: [Start Consequences] -> [Standard Narrative Beats] -> [End Consequences]
        var mergedBeats = MergeBeats(startConsequenceBeats, beatsDto, endConsequenceBeats);

        // 7. Save changes & construct response DTO using Mapster
        await unitOfWork.SaveAsync();

        var narrativeFlowDto = new NarrativeFlowDto
        {
            ShiftId = shift!.ShiftId,
            Shift   = shift.Adapt<ShiftDto>(),
            Beats   = mergedBeats
        };

        return Result.Success(narrativeFlowDto);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Shift Management (Admin)
    // ═══════════════════════════════════════════════════════════════════════

    public async Task<Result<ShiftDetailDto>> CreateShift(CreateShiftDto dto)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(dto.Title))
            return Result.Failure<ShiftDetailDto>(NarrativeErrors.ShiftTitleRequired);

        if (dto.ChapterNumber <= 0)
            return Result.Failure<ShiftDetailDto>(NarrativeErrors.InvalidChapterNumber);

        if (dto.ShiftNumber <= 0)
            return Result.Failure<ShiftDetailDto>(NarrativeErrors.InvalidShiftNumber);

        // Enforce unique (ChapterNumber, ShiftNumber)
        var duplicate = await unitOfWork.GetRepository<Shift>()
            .FindAsync(s => s.ChapterNumber == dto.ChapterNumber &&
                            s.ShiftNumber   == dto.ShiftNumber);

        if (duplicate != null)
            return Result.Failure<ShiftDetailDto>(NarrativeErrors.DuplicateShiftNumber);

        var shift = dto.Adapt<Shift>();
        shift.Title       = dto.Title.Trim();           // Trim before persisting
        shift.Description = dto.Description?.Trim();
        shift.CreatedAt   = DateTime.UtcNow;

        await unitOfWork.GetRepository<Shift>().AddAsync(shift);
        await unitOfWork.SaveAsync();

        return Result.Success(MapToShiftDetailDto(shift));
    }

    public async Task<Result<ShiftDetailDto>> GetShift(int shiftId)
    {
        var shift = await unitOfWork.GetRepository<Shift>()
            .FindAsync(s => s.ShiftId == shiftId, ["StoryBeats", "StoryBeats.Choices"]);

        if (shift == null)
            return Result.Failure<ShiftDetailDto>(NarrativeErrors.ShiftNotFound);

        return Result.Success(MapToShiftDetailDto(shift));
    }

    public Task<Result<List<ShiftDto>>> GetAllShifts()
    {
        var shifts = unitOfWork.GetRepository<Shift>()
            .FindAll(s => true)
            .OrderBy(s => s.ChapterNumber)
            .ThenBy(s => s.ShiftNumber)
            .ToList();

        var dtos = shifts.Adapt<List<ShiftDto>>();
        return Task.FromResult(Result.Success(dtos));
    }

    public async Task<Result<ShiftDetailDto>> UpdateShift(int shiftId, UpdateShiftDto dto)
    {
        var shift = unitOfWork.GetRepository<Shift>()
            .FindWithTracking(s => s.ShiftId == shiftId);

        if (shift == null)
            return Result.Failure<ShiftDetailDto>(NarrativeErrors.ShiftNotFound);

        // Validate title
        if (dto.Title != null && string.IsNullOrWhiteSpace(dto.Title))
            return Result.Failure<ShiftDetailDto>(NarrativeErrors.ShiftTitleRequired);

        // Validate chapter/shift numbers
        if (dto.ChapterNumber.HasValue && dto.ChapterNumber.Value <= 0)
            return Result.Failure<ShiftDetailDto>(NarrativeErrors.InvalidChapterNumber);

        if (dto.ShiftNumber.HasValue && dto.ShiftNumber.Value <= 0)
            return Result.Failure<ShiftDetailDto>(NarrativeErrors.InvalidShiftNumber);

        // Check uniqueness if either number is changing
        int targetChapter = dto.ChapterNumber ?? shift.ChapterNumber;
        int targetShift   = dto.ShiftNumber   ?? shift.ShiftNumber;

        bool numberChanging = targetChapter != shift.ChapterNumber ||
                              targetShift   != shift.ShiftNumber;

        if (numberChanging)
        {
            var conflicting = await unitOfWork.GetRepository<Shift>()
                .FindAsync(s => s.ChapterNumber == targetChapter &&
                                s.ShiftNumber   == targetShift   &&
                                s.ShiftId       != shiftId);

            if (conflicting != null)
                return Result.Failure<ShiftDetailDto>(NarrativeErrors.DuplicateShiftNumber);
        }

        // Apply changes
        if (dto.Title != null)
            shift.Title = dto.Title.Trim();

        if (dto.Description != null)
            shift.Description = dto.Description.Trim();

        if (dto.ChapterNumber.HasValue)
            shift.ChapterNumber = dto.ChapterNumber.Value;

        if (dto.ShiftNumber.HasValue)
            shift.ShiftNumber = dto.ShiftNumber.Value;

        if (dto.IsCapstone.HasValue)
            shift.IsCapstone = dto.IsCapstone.Value;

        if (dto.ClearUnlockCondition)
            shift.UnlockCondition = null;
        else if (dto.UnlockCondition != null)
            shift.UnlockCondition = dto.UnlockCondition;

        await unitOfWork.GetRepository<Shift>().UpdateAsync(shift);
        await unitOfWork.SaveAsync();

        // Re-load beats for the response
        var refreshed = await unitOfWork.GetRepository<Shift>()
            .FindAsync(s => s.ShiftId == shiftId, ["StoryBeats", "StoryBeats.Choices"]);

        return Result.Success(MapToShiftDetailDto(refreshed!));
    }

    public async Task<Result> DeleteShift(int shiftId)
    {
        var shift = await unitOfWork.GetRepository<Shift>()
            .FindAsync(s => s.ShiftId == shiftId, ["StoryBeats", "ShiftProgresses"]);

        if (shift == null)
            return Result.Failure(NarrativeErrors.ShiftNotFound);

        // Guard: cannot delete if players have progress history on this shift
        if (shift.ShiftProgresses.Count > 0)
            return Result.Failure(NarrativeErrors.ShiftHasPlayerProgress);

        // Guard: cannot delete if beats are still assigned (Restrict FK)
        if (shift.StoryBeats.Count > 0)
            return Result.Failure(NarrativeErrors.ShiftHasStoryBeats);

        unitOfWork.GetRepository<Shift>().Delete(shift);
        await unitOfWork.SaveAsync();

        return Result.Success();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // StoryBeat Management (Admin)
    // ═══════════════════════════════════════════════════════════════════════

    public async Task<Result<BeatDto>> CreateStoryBeat(CreateStoryBeatDto dto)
    {
        // Validate shift exists
        var shift = await unitOfWork.GetRepository<Shift>()
            .FindAsync(s => s.ShiftId == dto.ShiftId);

        if (shift == null)
            return Result.Failure<BeatDto>(NarrativeErrors.ShiftNotFound);

        // Validate BeatKey uniqueness
        if (string.IsNullOrWhiteSpace(dto.BeatKey))
            return Result.Failure<BeatDto>(NarrativeErrors.BeatKeyRequired);

        var existingKey = await unitOfWork.GetRepository<StoryBeat>()
            .FindAsync(b => b.BeatKey == dto.BeatKey.Trim());

        if (existingKey != null)
            return Result.Failure<BeatDto>(NarrativeErrors.DuplicateBeatKey);

        // Validate content
        if (dto.ContentJson == null || string.IsNullOrWhiteSpace(dto.ContentJson.Text))
            return Result.Failure<BeatDto>(NarrativeErrors.ContentTextRequired);

        // Type-specific rules
        if (dto.BeatType == BeatType.Narrative)
        {
            if (!dto.SequenceOrder.HasValue)
                return Result.Failure<BeatDto>(NarrativeErrors.SequenceOrderRequiredForNarrativeBeat);

            // Check for sequence order conflict in the target shift
            var seqConflict = await unitOfWork.GetRepository<StoryBeat>()
                .FindAsync(b => b.ShiftId       == dto.ShiftId &&
                                b.BeatType      == BeatType.Narrative &&
                                b.SequenceOrder == dto.SequenceOrder.Value);

            if (seqConflict != null)
                return Result.Failure<BeatDto>(NarrativeErrors.SequenceOrderConflict);
        }
        else // Consequence
        {
            if (dto.SequenceOrder.HasValue)
                return Result.Failure<BeatDto>(NarrativeErrors.SequenceOrderRequiredForNarrativeBeat);

            if (string.IsNullOrWhiteSpace(dto.InjectPosition) ||
                (dto.InjectPosition != "start" && dto.InjectPosition != "end"))
                return Result.Failure<BeatDto>(NarrativeErrors.InvalidInjectPosition);
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            var beat = new StoryBeat
            {
                ShiftId       = dto.ShiftId,
                BeatKey       = dto.BeatKey.Trim(),
                BeatType      = dto.BeatType,
                SequenceOrder = dto.BeatType == BeatType.Narrative ? dto.SequenceOrder : null,
                App           = dto.App,
                SenderName    = dto.SenderName,
                ContentJson   = dto.ContentJson,
                DesktopEvent  = dto.DesktopEvent,
                DelaySeconds  = dto.DelaySeconds,
                HasChoices    = dto.HasChoices,
                CreatedAt     = DateTime.UtcNow
            };

            await unitOfWork.GetRepository<StoryBeat>().AddAsync(beat);
            await unitOfWork.SaveAsync();

            // For consequence beats, also create the Consequence row
            if (dto.BeatType == BeatType.Consequence)
            {
                var consequence = new Consequence
                {
                    BeatId         = beat.BeatId,
                    InjectPosition = dto.InjectPosition!
                };
                await unitOfWork.GetRepository<Consequence>().AddAsync(consequence);
                await unitOfWork.SaveAsync();
            }

            await unitOfWork.CommitAsync();

            // Re-fetch with choices for a full DTO
            var saved = await unitOfWork.GetRepository<StoryBeat>()
                .FindAsync(b => b.BeatId == beat.BeatId, ["Choices"]);

            return Result.Success(saved!.Adapt<BeatDto>());
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<Result<BeatDto>> GetStoryBeat(int beatId)
    {
        var beat = await unitOfWork.GetRepository<StoryBeat>()
            .FindAsync(b => b.BeatId == beatId, ["Choices"]);

        if (beat == null)
            return Result.Failure<BeatDto>(NarrativeErrors.BeatNotFound);

        return Result.Success(beat.Adapt<BeatDto>());
    }

    public async Task<Result<BeatDto>> UpdateStoryBeat(int beatId, UpdateStoryBeatDto dto)
    {
        var beat = unitOfWork.GetRepository<StoryBeat>()
            .FindWithTracking(b => b.BeatId == beatId);

        if (beat == null)
            return Result.Failure<BeatDto>(NarrativeErrors.BeatNotFound);

        // Determine effective values after applying dto
        int    targetShiftId      = dto.ShiftId       ?? beat.ShiftId;
        BeatType targetBeatType   = dto.BeatType      ?? beat.BeatType;
        int?   targetSequenceOrder = dto.SequenceOrder ?? beat.SequenceOrder;

        // Validate target shift
        if (dto.ShiftId.HasValue && dto.ShiftId.Value != beat.ShiftId)
        {
            var targetShift = await unitOfWork.GetRepository<Shift>()
                .FindAsync(s => s.ShiftId == dto.ShiftId.Value);

            if (targetShift == null)
                return Result.Failure<BeatDto>(NarrativeErrors.ShiftNotFound);

            // Guard: consequence beats with active queue entries cannot be moved
            if (beat.BeatType == BeatType.Consequence)
            {
                var activeQueues = unitOfWork.GetRepository<ConsequenceQueue>()
                    .FindAll(cq => cq.Consequence.BeatId == beatId &&
                                   cq.Status == ConsequenceStatus.pending)
                    .Any();

                if (activeQueues)
                    return Result.Failure<BeatDto>(NarrativeErrors.ConsequenceBeatCannotChangeShift);
            }
        }

        // Validate BeatType change and SequenceOrder consistency
        if (targetBeatType == BeatType.Narrative)
        {
            if (!targetSequenceOrder.HasValue)
                return Result.Failure<BeatDto>(NarrativeErrors.SequenceOrderRequiredForNarrativeBeat);

            // Check for sequence conflict — exclude this beat itself
            bool orderChanging = dto.SequenceOrder.HasValue &&
                                 (dto.SequenceOrder.Value != beat.SequenceOrder ||
                                  targetShiftId           != beat.ShiftId);

            if (orderChanging)
            {
                var seqConflict = await unitOfWork.GetRepository<StoryBeat>()
                    .FindAsync(b => b.ShiftId       == targetShiftId       &&
                                    b.BeatType      == BeatType.Narrative  &&
                                    b.SequenceOrder == targetSequenceOrder &&
                                    b.BeatId        != beatId);

                if (seqConflict != null)
                    return Result.Failure<BeatDto>(NarrativeErrors.SequenceOrderConflict);
            }
        }
        else // Consequence
        {
            // Consequence beats must not have a SequenceOrder
            if (targetSequenceOrder.HasValue)
                return Result.Failure<BeatDto>(NarrativeErrors.SequenceOrderRequiredForNarrativeBeat);

            // Validate InjectPosition if being changed
            if (dto.InjectPosition != null &&
                dto.InjectPosition != "start" &&
                dto.InjectPosition != "end")
                return Result.Failure<BeatDto>(NarrativeErrors.InvalidInjectPosition);
        }

        // Validate BeatKey uniqueness if changing
        if (dto.BeatType == BeatType.Narrative || dto.BeatType == BeatType.Consequence)
        {
            // BeatKey itself is not in UpdateStoryBeatDto — beats cannot change their key
            // (it is the Ink bridge lookup identifier; treat it as immutable after creation)
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            // Apply scalar changes
            if (dto.ShiftId.HasValue)
                beat.ShiftId = dto.ShiftId.Value;

            if (dto.BeatType.HasValue)
            {
                beat.BeatType = dto.BeatType.Value;
                // When switching to Consequence, clear sequence order
                if (dto.BeatType.Value == BeatType.Consequence)
                    beat.SequenceOrder = null;
            }

            if (dto.SequenceOrder.HasValue && beat.BeatType == BeatType.Narrative)
                beat.SequenceOrder = dto.SequenceOrder.Value;

            if (dto.App.HasValue)
                beat.App = dto.App.Value;

            if (dto.SenderName != null)
                beat.SenderName = dto.SenderName;

            if (dto.ContentJson != null)
                beat.ContentJson = dto.ContentJson;

            if (dto.DesktopEvent != null)
                beat.DesktopEvent = dto.DesktopEvent;

            if (dto.DelaySeconds.HasValue)
                beat.DelaySeconds = dto.DelaySeconds.Value;

            if (dto.HasChoices.HasValue)
                beat.HasChoices = dto.HasChoices.Value;

            await unitOfWork.GetRepository<StoryBeat>().UpdateAsync(beat);

            // Update the Consequence row's InjectPosition if applicable
            if (beat.BeatType == BeatType.Consequence && dto.InjectPosition != null)
            {
                var consequence = unitOfWork.GetRepository<Consequence>()
                    .FindWithTracking(c => c.BeatId == beatId);

                if (consequence != null)
                {
                    consequence.InjectPosition = dto.InjectPosition;
                    await unitOfWork.GetRepository<Consequence>().UpdateAsync(consequence);
                }
            }

            await unitOfWork.SaveAsync();
            await unitOfWork.CommitAsync();

            var saved = await unitOfWork.GetRepository<StoryBeat>()
                .FindAsync(b => b.BeatId == beatId, ["Choices"]);

            return Result.Success(saved!.Adapt<BeatDto>());
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<Result> DeleteStoryBeat(int beatId)
    {
        var beat = await unitOfWork.GetRepository<StoryBeat>()
            .FindAsync(b => b.BeatId == beatId, ["Choices", "Consequence"]);

        if (beat == null)
            return Result.Failure(NarrativeErrors.BeatNotFound);

        // Guard 1: beats with choices that themselves reference consequences cannot be blindly deleted.
        // Choice -> StoryBeat is Cascade (EF will cascade from Beat), but we still verify
        // those choices do not have pending ConsequenceQueue entries.
        if (beat.Choices.Count > 0)
        {
            var choiceIds = beat.Choices.Select(c => c.ChoiceId).ToList();

            // Check if any Choice from this beat has a Consequence with pending queues
            bool hasActiveQueues = unitOfWork.GetRepository<ConsequenceQueue>()
                .FindAll(cq => cq.Status == ConsequenceStatus.pending)
                .Join(
                    unitOfWork.GetRepository<Consequence>().FindAll(c => true),
                    cq => cq.ConsequenceId,
                    c  => c.ConsequenceId,
                    (cq, c) => new { cq, c })
                .Any(x => x.c.Beat != null && beat.Choices
                    .Select(ch => ch.ConsequenceId)
                    .Where(id => id.HasValue)
                    .Contains(x.c.ConsequenceId));

            if (hasActiveQueues)
                return Result.Failure(NarrativeErrors.BeatHasActiveConsequenceQueues);
        }

        // Guard 2: if this IS a consequence beat, check whether the Consequence has pending queues
        if (beat.BeatType == BeatType.Consequence && beat.Consequence != null)
        {
            var pendingQueues = unitOfWork.GetRepository<ConsequenceQueue>()
                .FindAll(cq => cq.ConsequenceId == beat.Consequence.ConsequenceId &&
                               cq.Status        == ConsequenceStatus.pending)
                .Any();

            if (pendingQueues)
                return Result.Failure(NarrativeErrors.BeatHasActiveConsequenceQueues);
        }

        // Safe to delete — Choice rows cascade (OnDelete.Cascade) and
        // Consequence rows cascade (OnDelete.Cascade) from StoryBeat
        unitOfWork.GetRepository<StoryBeat>().Delete(beat);
        await unitOfWork.SaveAsync();

        return Result.Success();
    }

    public async Task<Result<BeatDto>> AssignBeatToShift(int beatId, int shiftId)
    {
        var beat = unitOfWork.GetRepository<StoryBeat>()
            .FindWithTracking(b => b.BeatId == beatId);

        if (beat == null)
            return Result.Failure<BeatDto>(NarrativeErrors.BeatNotFound);

        if (beat.ShiftId == shiftId)
            return Result.Failure<BeatDto>(NarrativeErrors.BeatAlreadyInShift);

        var targetShift = await unitOfWork.GetRepository<Shift>()
            .FindAsync(s => s.ShiftId == shiftId);

        if (targetShift == null)
            return Result.Failure<BeatDto>(NarrativeErrors.ShiftNotFound);

        // Consequence beats with active queue entries cannot be re-assigned
        if (beat.BeatType == BeatType.Consequence)
        {
            var activeQueues = unitOfWork.GetRepository<ConsequenceQueue>()
                .FindAll(cq => cq.Consequence.BeatId == beatId &&
                               cq.Status == ConsequenceStatus.pending)
                .Any();

            if (activeQueues)
                return Result.Failure<BeatDto>(NarrativeErrors.ConsequenceBeatCannotChangeShift);
        }

        // For narrative beats: check for sequence order conflict in the target shift
        if (beat.BeatType == BeatType.Narrative && beat.SequenceOrder.HasValue)
        {
            var seqConflict = await unitOfWork.GetRepository<StoryBeat>()
                .FindAsync(b => b.ShiftId       == shiftId          &&
                                b.BeatType      == BeatType.Narrative &&
                                b.SequenceOrder == beat.SequenceOrder &&
                                b.BeatId        != beatId);

            if (seqConflict != null)
                return Result.Failure<BeatDto>(NarrativeErrors.SequenceOrderConflict);
        }

        beat.ShiftId = shiftId;
        await unitOfWork.GetRepository<StoryBeat>().UpdateAsync(beat);
        await unitOfWork.SaveAsync();

        var saved = await unitOfWork.GetRepository<StoryBeat>()
            .FindAsync(b => b.BeatId == beatId, ["Choices"]);

        return Result.Success(saved!.Adapt<BeatDto>());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private helpers — Runtime
    // ═══════════════════════════════════════════════════════════════════════

    private async Task<(Shift? shift, Result<NarrativeFlowDto>? failure)> GetShiftEntityForRuntime(int shiftId)
    {
        var shift = await unitOfWork.GetRepository<Shift>()
            .FindAsync(s => s.ShiftId == shiftId);

        if (shift == null)
            return (null, Result.Failure<NarrativeFlowDto>(NarrativeErrors.ShiftNotFound));

        return (shift, null);
    }

    private List<BeatDto> GetNarrativeBeats(int shiftId)
    {
        var narrativeBeats = unitOfWork.GetRepository<StoryBeat>()
            .FindAll(sb => sb.ShiftId == shiftId && sb.BeatType == BeatType.Narrative, ["Choices"])
            .OrderBy(sb => sb.SequenceOrder)
            .ToList();

        return narrativeBeats.Adapt<List<BeatDto>>();
    }

    private List<ConsequenceQueue> GetPendingConsequences(int playerId, int shiftId)
    {
        return unitOfWork.GetRepository<ConsequenceQueue>()
            .FindAll(cq => cq.PlayerId == playerId &&
                           cq.Status   == ConsequenceStatus.pending &&
                           cq.Consequence.Beat.ShiftId == shiftId,
                     ["Consequence", "Consequence.Beat", "Consequence.Beat.Choices"])
            .ToList();
    }

    private static List<BeatDto> MergeBeats(
        List<BeatDto> start,
        List<BeatDto> narrative,
        List<BeatDto> end)
    {
        var merged = new List<BeatDto>(start.Count + narrative.Count + end.Count);
        merged.AddRange(start);
        merged.AddRange(narrative);
        merged.AddRange(end);
        return merged;
    }

    private async Task<(List<BeatDto> start, List<BeatDto> end)>
        CategorizeConsequencesByInjectPosition(List<ConsequenceQueue> pendingConsequences)
    {
        var startBeats = new List<BeatDto>();
        var endBeats   = new List<BeatDto>();

        foreach (var cq in pendingConsequences)
        {
            if (cq.Consequence?.Beat != null)
            {
                var beatDto = cq.Consequence.Beat.Adapt<BeatDto>();

                if (string.Equals(cq.Consequence.InjectPosition, "end",
                        StringComparison.OrdinalIgnoreCase))
                    endBeats.Add(beatDto);
                else
                    startBeats.Add(beatDto);
            }

            cq.Status  = ConsequenceStatus.fired;
            cq.FiredAt = DateTime.UtcNow;
            await unitOfWork.GetRepository<ConsequenceQueue>().UpdateAsync(cq);
        }

        return (startBeats, endBeats);
    }

    private async Task<(Player?, Result<NarrativeFlowDto>?)> ValidatePlayerAccess(
        int playerId, int shiftId)
    {
        var player = await unitOfWork.GetRepository<Player>()
            .FindAsync(p => p.PlayerId == playerId);

        if (player == null)
            return (null, Result.Failure<NarrativeFlowDto>(ChoiceErrors.PlayerNotFound));

        if (player.CurrentShiftId != shiftId)
            return (null, Result.Failure<NarrativeFlowDto>(ChoiceErrors.ShiftMismatch));

        return (player, null);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Private helpers — Admin
    // ═══════════════════════════════════════════════════════════════════════

    private static ShiftDetailDto MapToShiftDetailDto(Shift shift)
        => shift.Adapt<ShiftDetailDto>();
}