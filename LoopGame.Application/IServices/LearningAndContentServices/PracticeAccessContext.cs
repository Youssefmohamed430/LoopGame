using LoopGame.Domain.Entities.Code;
using LoopGame.Domain.Entities.Player;

namespace LoopGame.Application.IServices.LearningAndContentServices;

/// <summary>
/// Validated access context returned after CheckAccess succeeds.
/// Bundles the pre-loaded entities so downstream steps never re-query them.
/// </summary>
public sealed record PracticeAccessContext(
    Player Player,
    PlayerShiftProgress ShiftProgress,
    int ShiftId);
