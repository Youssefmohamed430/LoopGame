using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Controllers;

/// <summary>
/// Player Practice & Code Submission Endpoints.
///
/// Route convention matches Section 7.3 of Backend Architecture Doc:
///   GET  api/practice/{playerId}/task/{taskId}
///   POST api/practice/{playerId}/submit
/// </summary>
[ApiController]
[Route("api/practice")]
public class PracticeController(IPracticeService _practiceService) : ControllerBase
{
    /// <summary>
    /// Retrieves a practice task with player-visible test cases.
    /// </summary>
    [HttpGet("{playerId:int}/task/{taskId:int}")]
    public async Task<ActionResult<PracticeDto>> GetTask(int playerId, int taskId)
        => await Handle(_practiceService.GetTaskAsync(taskId, playerId));

    /// <summary>
    /// Submits player code for execution, evaluation, tier calculation, and gate progression.
    /// </summary>
    [HttpPost("{playerId:int}/submit")]
    public async Task<ActionResult<CodeSubmitResponseDto>> SubmitCode(
        int playerId, [FromBody] CodeSubmitRequestDto request)
        => await Handle(_practiceService.SubmitCode(playerId, request));

    private async Task<ActionResult<T>> Handle<T>(Task<Result<T>> operation)
    {
        var result = await operation;
        if (result.IsFailure)
            return result.Error.ToActionResult<T>();
        return Ok(result.Value);
    }
}
