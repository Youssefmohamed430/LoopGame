using LoopGame.Application.Dtos.SideTaskDtos;

namespace LoopGame.Application.IServices.SystemAndUtilityServices;

public interface IAiSideTaskClient
{

    Task<Result<List<AiGeneratedTaskDto>>> GenerateAsync(AiGenerateRequest request, CancellationToken ct = default);
}
