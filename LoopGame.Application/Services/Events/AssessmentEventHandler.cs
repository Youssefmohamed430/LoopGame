using Hangfire;
using Microsoft.Extensions.Logging;

namespace LoopGame.Application.Services.Events;

/// <summary>
/// Event handler that routes game events to the Assessment subsystem via Hangfire background processing.
/// </summary>
public class AssessmentEventHandler(
    IBackgroundJobClient _backgroundJobs,
    ILogger<AssessmentEventHandler> _logger) : IEventHandler
{
    public void Handle(GameEventDto gameEvent)
    {
        try
        {
            _backgroundJobs.Enqueue<IAssessmentService>(
                svc => svc.RecordEventAsync(gameEvent.ToAssessmentDto(), CancellationToken.None));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to enqueue Hangfire assessment event {EventType} for player {PlayerId}",
                gameEvent.EventType, gameEvent.PlayerId);
        }
    }
}
