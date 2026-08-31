using LoopGame.Application.IServices.SystemAndUtilityServices;

namespace LoopGame.Application.Services.SystemAndUtilityServices;

public class SaveService(IUnitOfWork _unitOfWork) : ISaveService
{

    public async Task<Result<SaveResultDto>> SaveDesktopStateAsync(int playerId, SaveRequestDto dto)
    {



        return new SaveResultDto();
    }
    
}
