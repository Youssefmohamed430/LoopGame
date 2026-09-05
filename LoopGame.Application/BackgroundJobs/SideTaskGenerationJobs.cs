using Hangfire;
using LoopGame.Application.IServices.SystemAndUtilityServices;
using Microsoft.Extensions.Logging;

namespace LoopGame.Application.BackgroundJobs;

/// <summary>
/// Hangfire job definition for generating AI side tasks.
/// Triggered after Assessment mastery calculation finishes.
/// </summary>
public class SideTaskGenerationJobs(
    IScenarioGeneratorService _generator,
    ILogger<SideTaskGenerationJobs> _logger)
{
    [JobDisplayName("Generate AI Side Tasks — Player {0}")]
    public async Task GenerateSideTasksAsync(int playerId)
    {
        _logger.LogInformation(
            "SideTaskGenerationJob started for player {PlayerId}", playerId);

        var result = await _generator.GenerateForPlayerAsync(playerId);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "SideTaskGenerationJob failed for player {PlayerId}: {Error}",
                playerId, result.Error.Description);
        }
    }
}
