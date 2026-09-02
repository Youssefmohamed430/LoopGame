using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoopGame.Controllers;

/// <summary>
/// Admin Content Management for Practice Tasks & Test Cases.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/practice")]
public class PracticeAdminController(IPracticeService _practiceService) : ControllerBase
{
    /// <summary>
    /// Creates a new practice task.
    /// </summary>
    [HttpPost("tasks")]
    public ActionResult<PracticeDto> AddPracticeTask([FromBody] PracticeDto practice)
    {
        var result = _practiceService.AddPracticeTask(practice);
        if (result.IsFailure)
            return result.Error.ToActionResult();
        return Ok(result.Value);
    }

    /// <summary>
    /// Updates an existing practice task.
    /// </summary>
    [HttpPut("tasks/{taskId:int}")]
    public ActionResult<PracticeDto> UpdatePracticeTask(int taskId, [FromBody] PracticeDto practice)
    {
        var result = _practiceService.UpdatePracticeTask(taskId, practice);
        if (result.IsFailure)
            return result.Error.ToActionResult();
        return Ok(result.Value);
    }

    /// <summary>
    /// Adds a list of test cases to a practice task.
    /// </summary>
    [HttpPost("testcases")]
    public ActionResult<List<TestCaseDto>> AddTestCases([FromBody] List<TestCaseDto> testCaseDtos)
    {
        var result = _practiceService.AddTestCasesAtPracticeTask(testCaseDtos);
        if (result.IsFailure)
            return result.Error.ToActionResult();
        return Ok(result.Value);
    }

    /// <summary>
    /// Updates an existing test case.
    /// </summary>
    [HttpPut("testcases/{testId:int}")]
    public ActionResult<TestCaseDto> UpdateTestCase(int testId, [FromBody] TestCaseDto testCaseDto)
    {
        var result = _practiceService.UpdateTestCasesAtPracticeTask(testId, testCaseDto);
        if (result.IsFailure)
            return result.Error.ToActionResult();
        return Ok(result.Value);
    }
}
