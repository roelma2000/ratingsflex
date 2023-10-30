using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ratingsflex.Areas.Movies.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ratingsflex.Areas.Movies.Data
{
    public class DynamoDbService : IDynamoDbService
    {
        private readonly DynamoDBContext _context;
        private readonly ILogger<DynamoDbService> _logger;
        private readonly IAmazonDynamoDB _client;

        public DynamoDbService(IAmazonDynamoDB dynamoDbClient, ILogger<DynamoDbService> logger)
        {
            _client = dynamoDbClient ?? throw new ArgumentNullException(nameof(dynamoDbClient));
            _context = new DynamoDBContext(_client);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<(List<MovieItem>, int)> GetMoviesByUploaderUserId(string uploaderUserId, int page = 1, int pageSize = 10)
        {
            var stopwatch = Stopwatch.StartNew();

            var queryRequest = new QueryRequest
            {
                TableName = "Movies",
                IndexName = "UploaderUserIdIndex",
                KeyConditionExpression = "uploaderUserId = :v_uploaderUserId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
        {
            { ":v_uploaderUserId", new AttributeValue { S = uploaderUserId } }
        }
            };

            var response = await _client.QueryAsync(queryRequest);
            var movies = new List<MovieItem>();

            foreach (var item in response.Items)
            {
                var movie = _context.FromDocument<MovieItem>(Document.FromAttributeMap(item));
                movies.Add(movie);
            }

            int totalItems = movies.Count;
            var paginatedMovies = movies.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            stopwatch.Stop();
            _logger.LogInformation($"Query took {stopwatch.ElapsedMilliseconds} ms");

            return (paginatedMovies, totalItems);
        }


        public async Task AddMovie(MovieItem movie)
        {
            await _context.SaveAsync(movie);
        }

        public async Task<List<MovieItem>> GetMoviesById(string movieId)
        {
            var conditions = new List<ScanCondition>
            {
                new ScanCondition("movieId", ScanOperator.Equal, movieId)
            };

            var movies = await _context.ScanAsync<MovieItem>(conditions).GetRemainingAsync();

            return movies.ToList();
        }

        public async Task<bool> DeleteMovieByMovieId(string movieId, string releaseTime)
        {
            try
            {
                var key = new Dictionary<string, AttributeValue>
                    {
                        { "movieId", new AttributeValue { S = movieId } },
                        { "releaseTime", new AttributeValue { S = releaseTime } }
                    };

                var request = new DeleteItemRequest
                {
                    TableName = "Movies",
                    Key = key
                };

                Console.WriteLine($"Delete request: {JsonConvert.SerializeObject(request)}");
                var response = await _client.DeleteItemAsync(request);

                return response.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (AmazonDynamoDBException e)
            {
                _logger.LogError($"Failed to delete movie with MovieId: {movieId}. Exception: {e.Message}");
                return false;
            }
            return true;

        }


        public async Task<MovieItem> GetMovieByMovieId(string movieId)
        {
            try
            {
                var movie = await _context.LoadAsync<MovieItem>(movieId);
                return movie;
            }
            catch (AmazonDynamoDBException e)
            {
                _logger.LogError($"Failed to get movie with MovieId: {movieId}. Exception: {e.Message}");
                return null;
            }
        }

        public async Task UpdateMovie(MovieItem movie)
        {
            try
            {
                await _context.SaveAsync(movie);
            }
            catch (AmazonDynamoDBException e)
            {
                _logger.LogError($"Failed to update movie with MovieId: {movie.MovieId}. Exception: {e.Message}");
                throw;
            }
        }



    }
}
