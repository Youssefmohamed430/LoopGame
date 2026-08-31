namespace LoopGame.Application.IServices.SystemAndUtilityServices;

public interface ISaveService
{
    Task<Result<SaveResultDto>> SaveDesktopStateAsync(int playerId, SaveRequestDto dto);

}
