
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Hangfire;
using LoopGame.Application.Dtos.AdminDtos;
using LoopGame.Application.IServices.SystemAndUtilityServices;
using LoopGame.Domain.Enums.AuthModule;
using LoopGame.Infrastructure.Identity;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace LoopGame.Application.Services.SystemAndUtilityServices;

public class AdminService : IAdminService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;
    public AdminService(IFileStorageService fileStorageService,IUnitOfWork unitOfWork, IMapper mapper,UserManager<ApplicationUser> userManager)
    {
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _userManager = userManager;
    }   
    public async Task<Result> UploadAsync(Concept concept, int uploadedBy, IFormFile file)
    {
        var existingFile = await _unitOfWork.GetRepository<SheetFile>().FindAll(f => f.Concept == concept && f.FileName == file.FileName)
                                                                       .FirstOrDefaultAsync();
        if (existingFile != null) 
            return Result.Failure(FileErrors.FileAlreadyExists);
        
        await using var stream = file.OpenReadStream();

        var s3Key = await _fileStorageService.UploadAsync(stream, file.FileName, file.ContentType, "side-tasks/uploads");

        if (s3Key.IsFailure)
            return Result.Failure(s3Key.Error);

        var sideTaskFile = new SheetFile
        {
            Concept = concept,
            S3Key = s3Key.Value,
            FileName = file.FileName,
            UploadedAt = DateTime.UtcNow,
            UploadedByUserId = uploadedBy,
            Status = SheetFileStatus.Pending
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

        return Result.Success();
    }

    public async Task<Result<List<SheetFileDto>>> ListUploadedFilesAsync(Concept concept)
    {
        var files = await _unitOfWork.GetRepository<SheetFile>().FindAll(f => f.Concept == concept).OrderByDescending(f => f.UploadedAt).ToListAsync();
        var filesDto = _mapper.Map<List<SheetFileDto>>(files);
        return Result.Success(filesDto);
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

    public async Task<Result<List<PlayerShiftProgressDto>>> GetShiftStudentsProgressAsync(int shiftId)
    {
        var shift =  await _unitOfWork.GetRepository<Shift>()
            .FindAllThenInclude(s => s.ShiftId == shiftId, query => query.Include(s => s.ShiftProgresses)
            .ThenInclude(pp => pp.Player)).FirstOrDefaultAsync(); 
        
        if (shift is null)
            return Result.Failure<List<PlayerShiftProgressDto>>(NarrativeErrors.ShiftNotFound);

        var playerProgresses = shift.ShiftProgresses.Select(sp => new PlayerShiftProgressDto
        {
            PlayerId = sp.PlayerId,
            PlayerName = sp.Player.PlayerName,
            Status = sp.Status.ToString()
        }).ToList();

        return Result.Success(playerProgresses);
    }

    public async Task<Result<PlayerOverallProgressDto>> GetStudentOverallProgressAsync(int playerId)
    {
        var player = await _unitOfWork.GetRepository<Player>().FindAllThenInclude(p => p.PlayerId == playerId, query => query.Include(p => p.ShiftProgresses)
            .ThenInclude(sp => sp.Shift)).FirstOrDefaultAsync();
        if (player is null)
            return Result.Failure<PlayerOverallProgressDto>(ChoiceErrors.PlayerNotFound);
        var totalShifts = await _unitOfWork.GetRepository<Shift>().GetAll<Shift>().CountAsync();
        var currentShift = player.ShiftProgresses.FirstOrDefault(sp => sp.Status == ShiftProgressStatus.InProgress);
        var completedShifts = player.ShiftProgresses.Count(s => s.Status == ShiftProgressStatus.Completed);
        var shifts = player.ShiftProgresses.Select(sp => new PlayerShiftProgressDetailsDto
        {
            ShiftId = sp.ShiftId,
            ShiftName = sp.Shift.Title,
            Status = sp.Status.ToString()
        }).ToList();
        var overallProgress = new PlayerOverallProgressDto
        {
            PlayerId = player.PlayerId,
            PlayerName = player.PlayerName,
            CurrentShiftId = currentShift?.ShiftId,
            CurrentShiftName = currentShift?.Shift.Title ?? "there no shift in progress", 
            CompletedShifts = completedShifts,
            TotalShifts = totalShifts,
            Shifts = shifts
        };
        return Result.Success(overallProgress);
    }
}
