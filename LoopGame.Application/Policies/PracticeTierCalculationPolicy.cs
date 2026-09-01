using LoopGame.Application.IServices.LearningAndContentServices;
using LoopGame.Domain.Enums;
using LoopGame.Domain.ValueObjects;

namespace LoopGame.Application.Services.LearningAndContentServices;

/// <summary>
/// Calculates the Practice Tier from code-execution results.
/// Extracted from PracticeService.CalculateTier().
///
/// Rules (preserved exactly from the original implementation):
///   - Empty results  → Mistake
///   - All tests pass → Ideal
///     (Acceptable tier requires future code-quality analysis — see NOTE below)
///   - Some pass      → Debt
///   - None pass      → Mistake
///
/// NOTE: Distinguishing Ideal vs Acceptable via code-quality metrics is a
/// documented future extension point. The method returns Ideal for 100% pass
/// rate until that analysis is implemented. The interface contract documents
/// this explicitly.
/// </summary>
public sealed class PracticeTierCalculationPolicy : ITierCalculationPolicy
{
    public ChoiceTier Calculate(IReadOnlyList<TestCaseResult> results)
    {
        if (results.Count == 0)
            return ChoiceTier.Mistake;

        // Fix: use (double) cast to avoid integer division truncation.
        int passed = results.Count(r => r.Passed);
        int total  = results.Count;

        if (passed == total)
            return ChoiceTier.Ideal;   // Acceptable = future extension

        if (passed > 0)
            return ChoiceTier.Debt;

        return ChoiceTier.Mistake;
    }
}
