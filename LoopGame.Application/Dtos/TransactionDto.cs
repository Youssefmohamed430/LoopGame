namespace LoopGame.Application.Dtos;

public record TransactionDto(
    int TransactionId,
    decimal Amount,
    TransactionType TransactionType,
    string Description,
    decimal BalanceAfter,
    DateTime CreatedAt);
