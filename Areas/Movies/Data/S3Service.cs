using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ratingsflex.Areas.Movies.Data
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<List<string>> GetAvailableMovies()
        {
            return await GetFileNamesFromBucket(_configuration["S3Buckets:ratingsflexmovies"]);
        }

        public async Task<List<string>> GetAvailablePosters()
        {
            return await GetFileNamesFromBucket(_configuration["S3Buckets:ratingsflexposters"]);
        }

        private async Task<List<string>> GetFileNamesFromBucket(string bucketName)
        {
            if (string.IsNullOrEmpty(bucketName))
            {
                Console.WriteLine("Bucket name is either null or empty.");
                return new List<string>();
            }

            var fileNames = new List<string>();
            try
            {
                var response = await _s3Client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = bucketName
                });

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    foreach (var s3Object in response.S3Objects)
                    {
                        fileNames.Add(s3Object.Key);
                    }
                }
                else
                {
                    Console.WriteLine($"Error getting files from bucket {bucketName}. HTTP Status Code: {response.HttpStatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting files from bucket {bucketName}. Exception: {ex.Message}");
            }
            return fileNames;
        }
    }
}
