using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ratingsflex.Areas.Movies.Data
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<DynamoDbService> _logger;

        public S3Service(IAmazonS3 s3Client, ILogger<DynamoDbService> logger)
        {
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> UploadFileAsync(IFormFile file, string bucketName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var fileKey = Guid.NewGuid().ToString() + "_" + file.FileName;

            using var stream = new MemoryStream();
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
                _logger.LogInformation("Successfully uploaded the FileId: {FileKey}.", fileKey);
                return fileKey;
            }
            else
            {
                throw new Exception("Failed to upload file to S3");
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

        public string GeneratePreSignedURL(string key, string bucketName, TimeSpan expiryDuration)
        {
            // Assuming you have an AmazonS3Client instance named _s3Client
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = key,
                Expires = DateTime.UtcNow.Add(expiryDuration),
                Verb = HttpVerb.GET
            };

            return _s3Client.GetPreSignedURL(request);
        }


        public async Task<Stream> GetFileAsync(string key, string bucketName)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = key
                };

                var response = await _s3Client.GetObjectAsync(request);
                return response.ResponseStream;
            }
            catch (AmazonS3Exception e)
            {
                // Handle AWS S3 specific exceptions here
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
                return null;
            }
            catch (Exception e)
            {
                // Handle generic exceptions here
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
                return null;
            }
        }


    }
}
