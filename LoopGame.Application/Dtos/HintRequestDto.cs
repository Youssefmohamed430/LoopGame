namespace LoopGame.Application.Dtos;

public record HintRequestDto(
    int TaskId,
    string TaskType,
    string? ConceptTag = null,
    string? ErrorMessage = null,
    string? CurrentCode = null);
