using LoopGame.Application.Dtos.NarrativeDtos;
using LoopGame.Domain.Abstractions;

namespace LoopGame.Application.IServices.LearningAndContentServices;

public interface INarrativeService
{
    // ── Runtime ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads the full narrative flow for a player starting a shift.
    /// Merges pending consequence beats with standard narrative beats.
    /// </summary>
    Task<Result<NarrativeFlowDto>> StartShift(int playerId, int shiftId);

    // ── Shift management (Admin) ──────────────────────────────────────────────

    /// <summary>Creates a new shift. Does NOT create any player progress records.</summary>
    Task<Result<ShiftDetailDto>> CreateShift(CreateShiftDto dto);

    /// <summary>Returns full shift detail including its ordered beats.</summary>
    Task<Result<ShiftDetailDto>> GetShift(int shiftId);

    /// <summary>Returns all shifts ordered by (ChapterNumber, ShiftNumber).</summary>
    Task<Result<List<ShiftDto>>> GetAllShifts();

    /// <summary>
    /// Updates editable shift metadata. Does NOT modify player runtime state.
    /// </summary>
    Task<Result<ShiftDetailDto>> UpdateShift(int shiftId, UpdateShiftDto dto);

    /// <summary>
    /// Deletes a shift only if it is safe to do so (no player progress, no story beats).
    /// Returns a conflict error instead of cascading if dependencies exist.
    /// </summary>
    Task<Result> DeleteShift(int shiftId);

    // ── StoryBeat management (Admin) ─────────────────────────────────────────

    /// <summary>
    /// Creates a new StoryBeat.
    /// For Narrative beats: SequenceOrder is required and must not conflict.
    /// For Consequence beats: SequenceOrder must be null; InjectPosition is required;
    /// a Consequence row is created automatically.
    /// </summary>
    Task<Result<BeatDto>> CreateStoryBeat(CreateStoryBeatDto dto);

    /// <summary>Returns a single beat by id.</summary>
    Task<Result<BeatDto>> GetStoryBeat(int beatId);

    /// <summary>
    /// Updates editable beat fields. Validates ordering constraints.
    /// Does NOT modify historical PlayerChoice or AssessmentEvent records.
    /// </summary>
    Task<Result<BeatDto>> UpdateStoryBeat(int beatId, UpdateStoryBeatDto dto);

    /// <summary>
    /// Deletes a beat only if safe: no active ConsequenceQueue entries,
    /// no Choice references that would orphan the narrative graph.
    /// </summary>
    Task<Result> DeleteStoryBeat(int beatId);

    /// <summary>
    /// Assigns (moves) an existing beat to a different shift.
    /// Validates shift existence and sequence ordering.
    /// Equivalent to setting StoryBeat.ShiftId.
    /// </summary>
    Task<Result<BeatDto>> AssignBeatToShift(int beatId, int shiftId);
}