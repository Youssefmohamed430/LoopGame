using LoopGame.Application.Dtos.AdminDtos;
using Microsoft.AspNetCore.Http;

namespace LoopGame.Application.IServices.SystemAndUtilityServices;

public interface IAdminService
{

    Task<Result> UploadAsync(Concept concept, int uploadedBy, IFormFile file);
    Task<Result<List<SheetFileDto>>> ListUploadedFilesAsync(Concept concept);
    Task<Result> DeleteUploadedFileAsync(int fileId);
    //Get players progress report for a specific shift
    Task<Result<List<PlayerShiftProgressDto>>> GetShiftStudentsProgressAsync(int shiftId);

    //Get player's progress overall report for a specific player
    Task<Result<PlayerOverallProgressDto>> GetStudentOverallProgressAsync(int playerId);

}
