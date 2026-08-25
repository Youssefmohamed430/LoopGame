namespace LoopGame.Application.Dtos;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    bool HasNext);
