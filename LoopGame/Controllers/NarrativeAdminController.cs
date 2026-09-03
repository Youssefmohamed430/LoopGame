using LoopGame.Application.Dtos.NarrativeDtos;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Controllers;

/// <summary>
/// Admin Narrative Content Management — Shifts and StoryBeats.
///
/// All endpoints here are for **admin/content-author** use only.
/// Players must not be able to reach these endpoints.
///
/// Authorization: TODO — once the RBAC pipeline lands, gate these endpoints
/// with [Authorize(Roles = "Admin")] (or the project's equivalent policy).
///
/// Route convention matches the existing EconomyController pattern:
///   /api/admin/shifts   — Shift management
///   /api/admin/beats    — StoryBeat management
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class NarrativeAdminController(
    INarrativeService _narrative,
    IChoiceService _choice) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════════════════
    // Shift endpoints
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Returns all shifts ordered by chapter then shift number.</summary>
    [HttpGet("shifts")]
    public async Task<ActionResult<List<ShiftDto>>> GetAllShifts()
        => await Handle(_narrative.GetAllShifts());

    /// <summary>Returns a single shift with its narrative and consequence beats.</summary>
    [HttpGet("shifts/{shiftId:int}")]
    public async Task<ActionResult<ShiftDetailDto>> GetShift(int shiftId)
        => await Handle(_narrative.GetShift(shiftId));

    /// <summary>Creates a new shift. Does NOT create any player progress records.</summary>
    [HttpPost("shifts")]
    public async Task<ActionResult<ShiftDetailDto>> CreateShift([FromBody] CreateShiftDto dto)
        => await Handle(_narrative.CreateShift(dto));

    /// <summary>
    /// Updates editable shift metadata (title, numbers, unlock condition, etc.).
    /// Does NOT modify player runtime state.
    /// </summary>
    [HttpPut("shifts/{shiftId:int}")]
    public async Task<ActionResult<ShiftDetailDto>> UpdateShift(
        int shiftId, [FromBody] UpdateShiftDto dto)
        => await Handle(_narrative.UpdateShift(shiftId, dto));

    /// <summary>
    /// Deletes a shift only if safe (no player progress records, no story beats).
    /// Returns 409 Conflict if dependencies exist.
    /// </summary>
    [HttpDelete("shifts/{shiftId:int}")]
    public async Task<ActionResult> DeleteShift(int shiftId)
    {
        var result = await _narrative.DeleteShift(shiftId);
        if (result.IsFailure)
            return result.Error.ToActionResult();
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // StoryBeat endpoints
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Returns a single story beat by id.</summary>
    [HttpGet("beats/{beatId:int}")]
    public async Task<ActionResult<BeatDto>> GetBeat(int beatId)
        => await Handle(_narrative.GetStoryBeat(beatId));

    /// <summary>
    /// Creates a new StoryBeat.
    /// For Narrative: SequenceOrder is required and must not conflict.
    /// For Consequence: SequenceOrder must be null; InjectPosition ('start'|'end') is required.
    /// A Consequence row is automatically created for consequence beats.
    /// </summary>
    [HttpPost("beats")]
    public async Task<ActionResult<BeatDto>> CreateBeat([FromBody] CreateStoryBeatDto dto)
        => await Handle(_narrative.CreateStoryBeat(dto));

    /// <summary>
    /// Updates editable beat fields.
    /// Validates ordering constraints and guards against breaking active player queues.
    /// Does NOT modify historical PlayerChoice or AssessmentEvent records.
    /// </summary>
    [HttpPut("beats/{beatId:int}")]
    public async Task<ActionResult<BeatDto>> UpdateBeat(
        int beatId, [FromBody] UpdateStoryBeatDto dto)
        => await Handle(_narrative.UpdateStoryBeat(beatId, dto));

    /// <summary>
    /// Assigns (moves) an existing beat to a different shift.
    /// Validates shift existence and sequence ordering.
    /// Will reject if the beat has active consequence queue entries.
    /// </summary>
    [HttpPut("beats/{beatId:int}/assign-shift/{shiftId:int}")]
    public async Task<ActionResult<BeatDto>> AssignBeatToShift(int beatId, int shiftId)
        => await Handle(_narrative.AssignBeatToShift(beatId, shiftId));

    /// <summary>
    /// Deletes a beat only if safe (no active ConsequenceQueue entries, no orphan choices).
    /// Returns 409 Conflict if historical player data would be corrupted.
    /// </summary>
    [HttpDelete("beats/{beatId:int}")]
    public async Task<ActionResult> DeleteBeat(int beatId)
    {
        var result = await _narrative.DeleteStoryBeat(beatId);
        if (result.IsFailure)
            return result.Error.ToActionResult();
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Choice endpoints
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates one or more choices for story beats.
    /// Validation: max 4 choices per beat, valid consequence references, no duplicates.
    /// </summary>
    [HttpPost("choices")]
    public async Task<ActionResult<List<ChoiceDto>>> AddChoice([FromBody] List<CreateChoiceDto> choices)
        => await Handle(_choice.AddChoice(choices));

    /// <summary>
    /// Updates editable choice properties.
    /// </summary>
    [HttpPut("choices/{choiceId:int}")]
    public async Task<ActionResult<ChoiceDto>> UpdateChoice(
        int choiceId, [FromBody] UpdateChoiceDto dto)
        => await Handle(_choice.UpdateChoice(choiceId, dto));

    // ═══════════════════════════════════════════════════════════════════════
    // Private helpers — mirrors the pattern used in EconomyController
    // ═══════════════════════════════════════════════════════════════════════

    private async Task<ActionResult<T>> Handle<T>(Task<Result<T>> operation)
    {
        var result = await operation;
        if (result.IsFailure)
            return result.Error.ToActionResult<T>();
        return Ok(result.Value);
    }
}
