using DocumentFormat.OpenXml.EMMA;
using Hangfire;
using LoopGame.Application.Dtos;
using LoopGame.Application.IServices.Events;
using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Domain.Entities.Player;
using Microsoft.Extensions.Logging;

namespace LoopGame.Application.BackgroundJobs;

/// <summary>
/// Hangfire job definitions for the assessment subsystem.
/// Jobs are thin orchestration wrappers — all business logic lives
/// in <see cref="IAssessmentService"/>.
/// </summary>
public class AssessmentJobs(
    IAssessmentService _assessment,
    IEventPublisher _eventPublisher,
    ILogger<AssessmentJobs> _logger)
{
    /// <summary>
    /// Triggered via Hangfire enqueue after a shift is completed.
    /// Computes mastery snapshots for all concepts the player interacted
    /// with during the specified shift.
    /// </summary>
    [JobDisplayName("Compute Mastery — Player {0}, Shift {1}")]
    public async Task ComputeMasteryJobAsync(int playerId, int shiftId)
    {
        _logger.LogInformation(
            "ComputeMasteryJob started for player {PlayerId}, shift {ShiftId}",
            playerId, shiftId);

        var result = await _assessment.ComputeMasteryAsync(playerId, shiftId);

        if (result.IsFailure)
        {
            _logger.LogWarning(
                "ComputeMasteryJob failed for player {PlayerId}, shift {ShiftId}: {Error}",
                playerId, shiftId, result.Error.Description);
            return;
        }

        // Publish assessment_completed event
        _eventPublisher.Publish(new GameEventDto(
            PlayerId: playerId,
            EventType: "assessment_completed",
            ConceptTag: null,
            Tier: null,
            PayloadJson: null));

        
    }
}
