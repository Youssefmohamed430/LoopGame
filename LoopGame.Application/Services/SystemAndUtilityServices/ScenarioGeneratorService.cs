using LoopGame.Application.Dtos.SideTaskDtos;
using LoopGame.Application.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Services.SystemAndUtilityServices
{
    public class ScenarioGeneratorService : IServices.SystemAndUtilityServices.IBackgroundJob
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileContentReaderService _fileContentReader = new FileContentReaderService();
        public ScenarioGeneratorService(IFileStorageService fileStorageService, IUnitOfWork unitOfWork, IFileContentReaderService fileContentReader)
        {
            _fileStorageService = fileStorageService;
            _unitOfWork = unitOfWork;
            _fileContentReader = fileContentReader;
        }
        public async Task<Result> ProcessAsync(int sheetFileId)
        {
            // 1. Get SheetFile
            var sheetFile = _unitOfWork.GetRepository<SheetFile>().FindAll(f => f.Id == sheetFileId).FirstOrDefault();
            if (sheetFile is null)
                throw new InvalidOperationException($"Sheet file with id {sheetFileId} was not found.");

            // 2. Download from S3
            var fileResult = await _fileStorageService.DownloadAsync(sheetFile.S3Key);
            if (fileResult is null)
                throw new InvalidOperationException($"File with S3 key {sheetFile.Id} was not found in storage.");
            await using var stream =  fileResult.Value;
            // 3. Read file
            var contentResult =  _fileContentReader.ReadAsync(stream,sheetFile.FileName);
            if(contentResult.IsFailure)
                return Result.Failure(contentResult.Error);
            var content = contentResult.Value; 
            // 5. Get reference scenario
            var referencesScenario = _unitOfWork.GetRepository<SideTaskTemplate>().GetAll<SideTaskReferenceScenarioRequest>().FirstOrDefault();
            // 6. Prepare request
            var request = new GenerateSideTaskRequest
            {
                ReferenceScenario = referencesScenario,
                ProblemsContent = content
            };
            // 7. call AI
            // 8. Validate
            // 9. Save generated scenarios
            return Result.Success();
        }
    }
}
