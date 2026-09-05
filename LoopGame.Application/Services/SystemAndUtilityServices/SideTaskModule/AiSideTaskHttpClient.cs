using System.Net.Http.Json;
using LoopGame.Application.Dtos.SideTaskDtos;
using LoopGame.Application.IServices.SystemAndUtilityServices;
using Microsoft.Extensions.Logging;

namespace LoopGame.Application.Services.SystemAndUtilityServices.SideTaskModule;

public class AiSideTaskHttpClient(
    HttpClient _http,
    ILogger<AiSideTaskHttpClient> _logger) : IAiSideTaskClient
{
    public async Task<Result<List<AiGeneratedTaskDto>>> GenerateAsync(AiGenerateRequest request, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("/generate", request, ct);
            response.EnsureSuccessStatusCode();
            
            var tasks = await response.Content.ReadFromJsonAsync<List<AiGeneratedTaskDto>>(cancellationToken: ct);
                
            return Result.Success(tasks ?? []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI service call failed for player {PlayerId}, concept {Concept}",
                request.PlayerId, request.Concept);
            return Result.Failure<List<AiGeneratedTaskDto>>(SideTaskErrors.AiCallFailed);
        }
    }
}
