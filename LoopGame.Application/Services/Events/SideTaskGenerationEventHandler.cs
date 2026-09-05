using Hangfire;
using LoopGame.Application.BackgroundJobs;
using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.Events;
using Microsoft.Extensions.Logging;

namespace LoopGame.Application.Services.Events;

/// <summary>
/// Event handler that listens for assessment completion events and enqueues side task generation jobs in Hangfire.
/// </summary>
public class SideTaskGenerationEventHandler(
    IBackgroundJobClient _backgroundJobs,
    ILogger<SideTaskGenerationEventHandler> _logger) : IEventHandler
{
    public void Handle(GameEventDto gameEvent)
    {
        if (gameEvent.EventType != "assessment_completed")
            return;

        try
        {
            _logger.LogInformation("Received assessment_completed event for player {PlayerId}. Enqueuing side task generation job.",
                gameEvent.PlayerId);

            _backgroundJobs.Enqueue<SideTaskGenerationJobs>(
                jobs => jobs.GenerateSideTasksAsync(gameEvent.PlayerId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to enqueue side task generation job for player {PlayerId}",
                gameEvent.PlayerId);
        }
    }
}
