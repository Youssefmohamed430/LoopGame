using LoopGame.Application.Dtos.SideTaskDtos;
using LoopGame.Application.Utilities;
using LoopGame.Domain.Enums.AuthModule;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.Services.SystemAndUtilityServices
{
    public class ScenarioGeneratorService : IScenarioGeneratorService
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileContentReaderService _fileContentReader;
        private readonly ILogger<ScenarioGeneratorService> _logger;
        public ScenarioGeneratorService(IFileStorageService fileStorageService, IUnitOfWork unitOfWork
                                        , IFileContentReaderService fileContentReader, ILogger<ScenarioGeneratorService> logger)
        {
            _fileStorageService = fileStorageService;
            _unitOfWork = unitOfWork;
            _fileContentReader = fileContentReader;
            _logger = logger;
        }
        public async Task<Result> ProcessAsync(int sheetFileId)
        {
            // 1. Get SheetFile
            var sheetFile = await _unitOfWork.GetRepository<SheetFile>().FindAll(f => f.Id == sheetFileId).FirstOrDefaultAsync();
            if (sheetFile is null)
            {
                _logger.LogError("SheetFile with ID {SheetFileId} was not found.", sheetFileId);
                return Result.Failure(FileErrors.FileNotFound);
            }
            try
            {
                _logger.LogInformation("Starting processing for SheetFile with ID {SheetFileId}.", sheetFileId);
                // 2. Update status to Processing
                sheetFile.Status = SheetFileStatus.Processing;
                await _unitOfWork.GetRepository<SheetFile>().UpdateAsync(sheetFile);
                await _unitOfWork.SaveAsync();
                _logger.LogInformation("SheetFile {SheetFileId} status changed to Processing.",sheetFileId);
                // 3. Download from S3
                var fileResult = await _fileStorageService.DownloadAsync(sheetFile.S3Key);
                if (fileResult.IsFailure)
                    throw new InvalidOperationException($"File with S3 key {sheetFile.S3Key} was not found in storage.");

                 await using var stream =  fileResult.Value;
                _logger.LogInformation("SheetFile {SheetFileId} downloaded successfully from S3.",sheetFileId);
                // 4. Read file
                var contentResult =  _fileContentReader.ReadFile(stream,sheetFile.FileName);
                if (contentResult.IsFailure)
                    throw new InvalidOperationException(contentResult.Error.Description);
                var content = contentResult.Value;
                _logger.LogInformation("Successfully extracted content from SheetFile {SheetFileId}.",sheetFileId);
                // 5. Get reference scenario
                var referenceScenario = _unitOfWork.GetRepository<SideTaskTemplate>().GetAll<SideTaskReferenceScenarioRequest>().FirstOrDefault();
                if (referenceScenario is null)
                    throw new InvalidOperationException("Reference scenario was not found.");
                // 6. Prepare request
                var request = new GenerateSideTaskRequest
                {
                    ReferenceScenario = referenceScenario,
                    ProblemsContent = content
                };
                // to do
                // 7. call AI
                // 8. Validate
                // 9. Save generated scenarios
                sheetFile.Status = SheetFileStatus.Completed;

                await _unitOfWork.GetRepository<SheetFile>().UpdateAsync(sheetFile);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("SheetFile {SheetFileId} processed successfully.",sheetFileId);

                return Result.Success();
            }
            catch (Exception ex)
            {
                sheetFile.Status = SheetFileStatus.Failed;

                await _unitOfWork.GetRepository<SheetFile>().UpdateAsync(sheetFile);
                await _unitOfWork.SaveAsync();

                _logger.LogError(ex,"Failed to process SheetFile {SheetFileId}.",sheetFileId);

                throw;
            }

        }
    }
}
