using Hangfire;
using LoopGame.Application.BackgroundJobs;
using Microsoft.Extensions.Logging;

namespace LoopGame.Application.Services.LearningAndContentServices;

/// <summary>
/// Hangfire implementation of <see cref="IAssessmentJobScheduler"/>.
/// Encapsulates job scheduling and exception handling so gameplay services remain clean and uncoupled from Hangfire.
/// </summary>
public class AssessmentJobScheduler(
    IBackgroundJobClient _backgroundJobs,
    ILogger<AssessmentJobScheduler> _logger) : IAssessmentJobScheduler
{
    public void EnqueueMasteryComputation(int playerId, int shiftId)
    {
        try
        {
            _backgroundJobs.Enqueue<AssessmentJobs>(
                jobs => jobs.ComputeMasteryJobAsync(playerId, shiftId));
        }
        catch (Exception ex)
        {
            // Assessment computation failure must never crash the gameplay flow.
            _logger.LogError(ex,
                "Failed to enqueue mastery computation job for player {PlayerId}, shift {ShiftId}",
                playerId, shiftId);
        }
    }
}
