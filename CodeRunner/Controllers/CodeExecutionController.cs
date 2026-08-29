using CodeRunner.Models;
using CodeRunner.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeRunner.Controllers;

[ApiController]
[Route("[controller]")]
public class CodeExecutionController : ControllerBase
{
    private readonly ICodeExecutionService _codeExecutionService;
    private readonly ILogger<CodeExecutionController> _logger;

    public CodeExecutionController(
        ICodeExecutionService codeExecutionService,
        ILogger<CodeExecutionController> logger)
    {
        _codeExecutionService = codeExecutionService;
        _logger = logger;
    }

    [HttpPost("/execute")]
    [ProducesResponseType(typeof(ExecuteCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Execute([FromBody] ExecuteCodeRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Execution request cannot be null." });
        }

        if (string.IsNullOrWhiteSpace(request.Language))
        {
            return BadRequest(new { message = "Field 'language' is required." });
        }

        if (string.IsNullOrWhiteSpace(request.SourceCode))
        {
            return BadRequest(new { message = "Field 'source_code' is required." });
        }

        _logger.LogInformation("Received POST /execute for language '{Language}' with {TestCaseCount} test cases",
            request.Language, request.TestCases?.Count ?? 0);

        var response = await _codeExecutionService.ExecuteAsync(request, cancellationToken);
        return Ok(response);
    }
}
