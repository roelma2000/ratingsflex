using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ratingsflex.Areas.Movies.Models;

namespace ratingsflex.Areas.Movies.Data
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DynamoDbService> _logger;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration, ILogger<DynamoDbService> logger)
        {
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> UploadFileAsync(IFormFile file, string bucketName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var fileKey = Guid.NewGuid().ToString() + "_" + file.FileName;

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                var putObjectRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileKey,
                    InputStream = stream,
                    ContentType = file.ContentType
                };

                var response = await _s3Client.PutObjectAsync(putObjectRequest);

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Successfully uploade the FileId: {fileKey}.");
                    return fileKey;
                }
                else
                {
                    throw new Exception("Failed to upload file to S3");
                }
            }
        }

        public async Task DeleteFileAsync(string fileKey, string bucketName)
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = fileKey
            };

            var response = await _s3Client.DeleteObjectAsync(deleteObjectRequest);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new Exception("Failed to delete file from S3");
            }
        }


    }
}
