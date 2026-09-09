namespace LoopGame.Application.Dtos;

public record HintRequestDto(
    int TaskId,
    string TaskType,
    Concept? ConceptTag = null,
    string? ErrorMessage = null,
    string? CurrentCode = null);
