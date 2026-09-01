using Hangfire;
using LoopGame.Application.IServices.EconomyAndProgressionServices;
using Microsoft.Extensions.Logging;

namespace LoopGame.Application.Services.EconomyAndProgressionServices;

/// <summary>
/// Hangfire-backed assessment event emitter. Replaces the former NoopAssessmentEventEmitter.
/// 
/// Each call to <see cref="Emit"/> fire-and-forgets a Hangfire background job that
/// persists the assessment event via <see cref="IAssessmentService.RecordEventAsync"/>.
/// The calling gameplay service's HTTP request returns immediately — assessment
/// persistence/computation never blocks the player's transaction.
/// 
/// Transactional safety: if the Hangfire enqueue itself fails, the gameplay
/// operation has already succeeded. The failure is logged but never propagated.
/// </summary>
public class HangfireAssessmentEventEmitter(
    IBackgroundJobClient _backgroundJobs,
    ILogger<HangfireAssessmentEventEmitter> _logger) : IAssessmentEventEmitter
{
    public void Emit(AssessmentEventDto assessmentEvent)
    {
        try
        {
            _backgroundJobs.Enqueue<IAssessmentService>(
                svc => svc.RecordEventAsync(assessmentEvent, CancellationToken.None));
        }
        catch (Exception ex)
        {
            // Assessment telemetry failure must never crash the gameplay flow.
            _logger.LogError(ex,
                "Failed to enqueue assessment event {EventType} for player {PlayerId}",
                assessmentEvent.EventType, assessmentEvent.PlayerId);
        }
    }
}
