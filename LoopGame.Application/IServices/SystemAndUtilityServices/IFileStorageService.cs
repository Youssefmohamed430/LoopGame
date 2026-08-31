using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopGame.Application.IServices.SystemAndUtilityServices
{
    public interface IFileStorageService
    {
        Task<Result<string>> UploadAsync(Stream fileStream,string fileName, string contentType,string folder);
        Task<Result<Stream>> DownloadAsync(string objectKey);

        Task<Result> DeleteAsync(string objectKey);
    }
}
