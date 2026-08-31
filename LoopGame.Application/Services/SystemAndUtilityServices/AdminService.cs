
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Hangfire;
using LoopGame.Application.IServices.SystemAndUtilityServices;
using Microsoft.AspNetCore.Http;

namespace LoopGame.Application.Services.SystemAndUtilityServices;

public class AdminService : IAdminService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    public AdminService(IFileStorageService fileStorageService,IUnitOfWork unitOfWork )
    {
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Result> UploadAsync(int shiftId, int uploadedBy, IFormFile file)
    {
        var shiftExists = await _unitOfWork.GetRepository<Shift>().FindAll(s => s.ShiftId == shiftId).AnyAsync();
        
        if (!shiftExists)
            return Result.Failure(AdminErrors.ShiftNotFound);

        var existingFile = await _unitOfWork.GetRepository<SheetFile>().FindAll(f => f.ShiftId == shiftId && f.FileName == file.FileName)
                                                                       .FirstOrDefaultAsync();
        if (existingFile != null) 
            return Result.Failure(FileErrors.FileAlreadyExists);
        
        await using var stream = file.OpenReadStream();

        var s3Key = await _fileStorageService.UploadAsync(stream, file.FileName, file.ContentType, "side-tasks/uploads");

        if (s3Key.IsFailure)
            return Result.Failure(s3Key.Error);

        var sideTaskFile = new SheetFile
        {
            ShiftId = shiftId,
            S3Key = s3Key.Value,
            FileName = file.FileName,
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = uploadedBy
        };
        try
        {
            await _unitOfWork.GetRepository<SheetFile>().AddAsync(sideTaskFile);
            await _unitOfWork.SaveAsync();
        }
        catch
        {
            await _fileStorageService.DeleteAsync(s3Key.Value);
            return Result.Failure(FileErrors.FileUploadFailed);
        }
        BackgroundJob.Enqueue<IServices.SystemAndUtilityServices.IBackgroundJob>(processor => processor.ProcessAsync(sideTaskFile.Id));

        return Result.Success();
    }
    public async Task<Result<List<SheetFile>>> ListUploadedFilesAsync(int shiftId)
    {
        var shiftExists = await _unitOfWork.GetRepository<Shift>().FindAll(s => s.ShiftId == shiftId).AnyAsync();
        
        if (!shiftExists)
            return Result.Failure<List<SheetFile>>(AdminErrors.ShiftNotFound);

        var files = await _unitOfWork.GetRepository<SheetFile>().FindAll(f => f.ShiftId == shiftId).OrderByDescending(f => f.UploadedAt).ToListAsync();

        return Result.Success(files);
    }
    public async Task<Result> DeleteUploadedFileAsync(int fileId)
    {
        var file = await _unitOfWork.GetRepository<SheetFile>().FindAll(f => f.Id == fileId).FirstOrDefaultAsync();
        
        if (file is null)
            return Result.Failure(FileErrors.FileNotFound);
        
        var deleteResult = await _fileStorageService.DeleteAsync(file.S3Key);

        if (deleteResult.IsFailure)
            return Result.Failure(deleteResult.Error);

        _unitOfWork.GetRepository<SheetFile>().Delete(file);// there a problem the deletion is not atomic with the S3 deletion. 
        await _unitOfWork.SaveAsync();
        return Result.Success();
    }

    //public async Task<Result<ShiftProgressReportDto>> GetShiftProgressReportAsync(int shiftId)
    //{
    //    var shift = await _unitOfWork.GetRepository<Shift>()
    //        .FindAll(s => s.ShiftId == shiftId)
    //        .Select(s => new { s.ShiftId, s.Title })
    //        .FirstOrDefaultAsync();

    //    if (shift is null)
    //        return Result.Failure<ShiftProgressReportDto>(AdminErrors.ShiftNotFound);


    //    var rows = await _unitOfWork.GetRepository<PlayerShiftProgress>()
    //        .FindAll(p => p.ShiftId == shiftId)
    //        .Select(p => new PlayerShiftProgressDto(
    //            p.PlayerId,
    //            $"Player#{p.PlayerId}",           // placeholder until identity navigation is added
    //            p.ShiftId,
    //            shift.Title,
    //            p.Status.ToString(),
    //            p.GateAttempts,
    //            p.StartedAt,
    //            p.CompletedAt))
    //        .ToListAsync();

    //    int completedCount   = rows.Count(r => r.Status == ShiftProgressStatus.Completed.ToString());
    //    int inProgressCount  = rows.Count(r => r.Status == ShiftProgressStatus.InProgress.ToString());

    //    return new ShiftProgressReportDto(
    //        shift.ShiftId,
    //        shift.Title,
    //        rows.Count,
    //        completedCount,
    //        inProgressCount,
    //        rows);
    //}

    //public async Task<Result<List<PlayerShiftProgressDto>>> GetPlayerProgressAsync(int playerId)
    //{
    //    var playerExists = await _unitOfWork.GetRepository<Player>()
    //        .FindAll(p => p.PlayerId == playerId)
    //        .AnyAsync();

    //    if (!playerExists)
    //        return Result.Failure<List<PlayerShiftProgressDto>>(AdminErrors.PlayerNotFound);

    //    var rows = await _unitOfWork.GetRepository<PlayerShiftProgress>()
    //        .FindAll(p => p.PlayerId == playerId)
    //        .OrderBy(p => p.Shift.ShiftNumber)
    //        .Select(p => new PlayerShiftProgressDto(
    //            p.PlayerId,
    //            $"Player#{p.PlayerId}",           // placeholder — see GetShiftProgressReportAsync note
    //            p.ShiftId,
    //            p.Shift.Title,
    //            p.Status.ToString(),
    //            p.GateAttempts,
    //            p.StartedAt,
    //            p.CompletedAt))
    //        .ToListAsync();

    //    return Result.Success(rows);
    //}
}
