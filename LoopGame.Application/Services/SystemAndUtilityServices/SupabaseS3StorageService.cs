using Amazon.S3;
using Amazon.S3.Model;
using LoopGame.Application.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoopGame.Application.Services.SystemAndUtilityServices
{
    public class SupabaseS3StorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly SupabaseS3Settings _settings;
        public SupabaseS3StorageService(IAmazonS3 s3Client,IOptions<SupabaseS3Settings> options)
        {
            _s3Client = s3Client;
            _settings = options.Value;
        }

        public async Task<Result<string>> UploadAsync(Stream fileStream, string fileName, string contentType, string folder)
        {
            var objectKey = $"{folder.TrimEnd('/')}/{Guid.NewGuid()}-{fileName}";
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = objectKey,
                InputStream = fileStream,
                ContentType = contentType
            };
            await _s3Client.PutObjectAsync( request );

            return Result.Success(objectKey);
        }
        public async Task<Result> DeleteAsync(string objectKey)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = objectKey
            };
            await _s3Client.DeleteObjectAsync(request);
            return Result.Success();
        }

        public async Task<Result<Stream>> DownloadAsync(string objectKey)
        {
            var request = new GetObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = objectKey
            };

            var response = await _s3Client.GetObjectAsync(request);

            return Result.Success(response.ResponseStream);
        }

    }
}
