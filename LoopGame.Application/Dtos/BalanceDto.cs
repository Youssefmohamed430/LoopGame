namespace LoopGame.Application.Dtos;

public record BalanceDto(
    decimal Balance,
    decimal TotalEarned,
    decimal TotalSpent,
    string SalaryTier);
