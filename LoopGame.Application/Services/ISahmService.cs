namespace LoopGame.Application.Services;

public interface ISahmService
{
    /// <summary>
    /// Validates the daily hint limit (with lazy midnight reset), consumes one
    /// hint and emits hint_request telemetry. Actual AI text generation is owned
    /// by the AI-pipeline group's IAiOrchestrationService (UC-SAHM-02/03/04).
    /// </summary>
    Task<Result<HintResponseDto>> RequestHintAsync(int playerId, HintRequestDto request, CancellationToken ct = default);

    /// <summary>Current tier, limits and remaining hints; Free defaults when no subscription exists (UC-SAHM-07).</summary>
    Task<Result<SahmStatusDto>> GetStatusAsync(int playerId, CancellationToken ct = default);

    /// <summary>Bulk reset for the midnight scheduler job (UC-SAHM-06); returns rows reset.</summary>
    Task<Result<int>> ResetExpiredCountersAsync(CancellationToken ct = default);
}
