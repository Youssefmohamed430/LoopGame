namespace LoopGame.Application.IServices.LearningAndContentServices;

/// <summary>
/// Service responsible for scheduling assessment background jobs (e.g. Hangfire jobs).
/// Decouples gameplay logic services (like PracticeService) from background job implementation details.
/// </summary>
public interface IAssessmentJobScheduler
{
    /// <summary>
    /// Enqueues a background job to compute concept mastery snapshots for a player and shift.
    /// </summary>
    void EnqueueMasteryComputation(int playerId, int shiftId);
}
