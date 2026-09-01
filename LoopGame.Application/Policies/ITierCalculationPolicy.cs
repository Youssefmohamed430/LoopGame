using LoopGame.Domain.Abstractions;
using LoopGame.Domain.Entities.Code;
using LoopGame.Domain.Enums;
using LoopGame.Domain.ValueObjects;

namespace LoopGame.Application.IServices.LearningAndContentServices;

/// <summary>
/// Determines the Practice Tier from a set of test-case execution results.
/// Isolated so the grading rule can be changed or extended without touching the orchestrator.
/// </summary>
public interface ITierCalculationPolicy
{
    /// <summary>
    /// Calculates the <see cref="ChoiceTier"/> from a collection of test-case results.
    ///
    /// Current rules (preserved from original implementation):
    ///   - All passed  → Ideal   (code-quality analysis is a future extension point)
    ///   - Some passed → Debt
    ///   - None passed → Mistake
    ///   - Empty set   → Mistake
    ///
    /// NOTE: Acceptable tier requires code-quality analysis not yet implemented.
    /// This is explicitly a future extension point, not an omission.
    /// </summary>
    ChoiceTier Calculate(IReadOnlyList<TestCaseResult> results);
}
