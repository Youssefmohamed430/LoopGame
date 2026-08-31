using LoopGame.Application.Dtos.AdminDtos;
using Microsoft.AspNetCore.Http;

namespace LoopGame.Application.IServices.SystemAndUtilityServices;

public interface IAdminService
{

    Task<Result> UploadAsync(int shiftId, int uploadedBy, IFormFile file);
    Task<Result<List<SheetFileDto>>> ListUploadedFilesAsync(int shiftId);
    Task<Result> DeleteUploadedFileAsync(int fileId);
}
