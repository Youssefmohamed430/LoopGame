namespace LoopGame.Application.IServices.EconomyAndProgressionServices;

public interface IEconomyService
{
    Task<Result<BalanceDto>> GetBalanceAsync(int playerId, CancellationToken ct = default);
    Task<Result<PagedResult<TransactionDto>>> GetTransactionHistoryAsync(int playerId, int page, int pageSize = 20, CancellationToken ct = default);
    Task<Result<decimal>> ApplyEgpDeltaAsync(int playerId, decimal delta, TransactionType type, string description, int? referenceId = null, CancellationToken ct = default);
    Task<Result<decimal>> PayShiftSalaryAsync(int playerId, int shiftId, CancellationToken ct = default);
    Task<Result> ResetEconomyAsync(int playerId, CancellationToken ct = default); // called by UC-GAME-11 flow
}
