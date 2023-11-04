using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Humanizer.Localisation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NuGet.ContentModel;
using ratingsflex.Areas.Movies.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ratingsflex.Areas.Movies.Data;
using Microsoft.EntityFrameworkCore;

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

        public async Task<(List<MovieItem>, int)> GetMoviesByUploaderUserId(string uploaderUserId, string genre = null, string rating = "All", int page = 1, int pageSize = 10)
        {
            var stopwatch = Stopwatch.StartNew();

            List<MovieItem> movies;

            if (!string.IsNullOrEmpty(genre) && genre != "All")
            {
                // Query the "GenreIndex" index to get movies of a specific genre
                var genreQueryRequest = new QueryRequest
                {
                    TableName = "Movies",
                    IndexName = "GenreIndex",
                    KeyConditionExpression = "genre = :v_genre",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":v_genre", new AttributeValue { S = genre } }
            }
                };

                var genreResponse = await _client.QueryAsync(genreQueryRequest);
                movies = new List<MovieItem>();

                foreach (var item in genreResponse.Items)
                {
                    var movie = _context.FromDocument<MovieItem>(Document.FromAttributeMap(item));
                    movies.Add(movie);
                }

                // Filter the results from step 1 to get movies uploaded by the specific user ID
                movies = movies.Where(m => m.UploaderUserId == uploaderUserId).ToList();
            }
            else
            {
                // Query the "UploaderUserIdIndex" index to get all movies uploaded by the specific user ID
                var userQueryRequest = new QueryRequest
                {
                    TableName = "Movies",
                    IndexName = "UploaderUserIdIndex",
                    KeyConditionExpression = "uploaderUserId = :v_uploaderUserId",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                { ":v_uploaderUserId", new AttributeValue { S = uploaderUserId } }
            }
                };

                var userResponse = await _client.QueryAsync(userQueryRequest);
                movies = new List<MovieItem>();

                foreach (var item in userResponse.Items)
                {
                    var movie = _context.FromDocument<MovieItem>(Document.FromAttributeMap(item));
                    movies.Add(movie);
                }
            }

            // Filter by rating if it's not "All"
            if (rating != "All")
            {
                movies = movies.Where(m => m.Rating >= int.Parse(rating)).ToList();
            }

            int totalItems = movies.Count;
            // Apply pagination
            var paginatedMovies = movies.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            stopwatch.Stop();
            _logger.LogInformation("Query took {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

            return (paginatedMovies, totalItems);
        }

        private CommentData ConvertToCommentData(Dictionary<string, AttributeValue> item)
        {
            return new CommentData
            {
                CommentText = item["commentText"].S,
                Timestamp = item["timestamp"].S,
                UserId = item["userId"].S
            };
        }

        public async Task<(List<ListAllMoviesViewModel>, int)> GetAllMovies(string genre = "All", string rating = "All", int page = 1, int pageSize = 10)
        {
            var stopwatch = Stopwatch.StartNew();

            List<ListAllMoviesViewModel> movies = new List<ListAllMoviesViewModel>();

            // Step 1: Query the "Movies" table to get all movies
            var scanRequest = new ScanRequest
            {
                TableName = "Movies",
            };

            var scanResponse = await _client.ScanAsync(scanRequest);

            foreach (var item in scanResponse.Items)
            {
                var document = Document.FromAttributeMap(item);
             
                // Manually map the document to ListAllMoviesViewModel
                var movie = new ListAllMoviesViewModel
                {
                    MovieId = document["movieId"].AsString(),
                    Title = document["title"].AsString(),
                    Description = document.ContainsKey("description") ? document["description"].AsString() : null,
                    ReleaseTime = DateTime.Parse(document["releaseTime"].AsString()),
                    Genre = document["genre"].AsString(),
                    MoviePath = document.ContainsKey("moviePath") ? document["moviePath"].AsString() : null,
                    PosterPath = document.ContainsKey("posterPath") ? document["posterPath"].AsString() : null,
                    UploaderUserId = document.ContainsKey("uploaderUserId") ? document["uploaderUserId"].AsString() : null,
                    Rating = double.Parse(document["rating"].AsString()),
                    // Add other fields as necessary
                };

                movies.Add(movie);
            }

            // Step 2: Filter the results based on the selected genre and rating
            if (!string.IsNullOrEmpty(genre) && genre != "All")
            {
                movies = movies.Where(m => m.Genre == genre).ToList();
            }

            if (!string.IsNullOrEmpty(rating) && rating != "All")
            {
                movies = movies.Where(m => m.Rating >= int.Parse(rating)).ToList();
            }

            int totalItems = movies.Count;
            var paginatedMovies = movies.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            stopwatch.Stop();
            _logger.LogInformation("Query took {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);

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
                _logger.LogError("Failed to delete movie with MovieId: {movieId}. Exception: {ErrorMessage}", movieId, e.Message);
                return false;
            }
            return true;

        }


        public async Task<MovieItem> GetMovieByMovieId(string movieId)
        {
            try
            {
                var conditions = new List<ScanCondition>
        {
            new ScanCondition("MovieId", ScanOperator.Equal, movieId),
        };

                var movies = await _context.ScanAsync<MovieItem>(conditions).GetRemainingAsync();

                // Convert the movies to JSON string and log it
                var moviesJson = JsonConvert.SerializeObject(movies, Formatting.Indented);
               // _logger.LogInformation("\nMovies returned from DynamoDB: {MoviesJson}\n", moviesJson);

                // This will return the first movie that matches the movieId. 
                return movies.FirstOrDefault();
            }
            catch (AmazonDynamoDBException e)
            {
                _logger.LogError("Failed to delete movie with MovieId: {movieId}. Exception: {ErrorMessage}", movieId, e.Message);
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
                _logger.LogError("Failed to delete movie with MovieId: {movieId}. Exception: {ErrorMessage}", movie.MovieId, e.Message);
                throw;
            }
        }

        // Convert from List<Dictionary<string, string>> to List<CommentItem>
        private List<CommentItem> ConvertToCommentItems(List<Dictionary<string, string>> commentDicts)
        {
            var commentItems = new List<CommentItem>();
            foreach (var dict in commentDicts)
            {
                var commentItem = new CommentItem
                {
                    CommentText = dict.ContainsKey("commentText") ? dict["commentText"] : null,
                    Timestamp = dict.ContainsKey("timestamp") ? dict["timestamp"] : null,
                    UserId = dict.ContainsKey("userId") ? dict["userId"] : null
                };
                commentItems.Add(commentItem);
            }
            return commentItems;
        }

        // Convert from List<CommentItem> to List<Dictionary<string, string>>
        public List<Dictionary<string, string>> ConvertToDictComments(List<CommentItem> commentItems)
        {
            var dictComments = new List<Dictionary<string, string>>();
            foreach (var comment in commentItems)
            {
                dictComments.Add(new Dictionary<string, string>
            {
                { "commentText", comment.CommentText },
                { "timestamp", comment.Timestamp },
                { "userId", comment.UserId }
            });
            }
            return dictComments;
        }
        public async Task<MovieItem> GetMovieDetails(string movieId)
        {
            try
            {
                // Retrieve the movie details from the database
                var movie = await GetMovieByMovieId(movieId);

                if (movie == null)
                {
                    _logger.LogError("Movie with MovieId: {movieId} not found.", movieId);
                    return null;
                }

                // Retrieve the comments for the movie
                var comments = await GetCommentsByMovieId(movieId);

                // Assign the list of CommentData objects to the movie's Comments property
                movie.Comments = comments;

                return movie;
            }
            catch (AmazonDynamoDBException e)
            {
                _logger.LogError("Failed to retrieve movie details for MovieId: {movieId}. Exception: {ErrorMessage}", movieId, e.Message);
                return null;
            }
        }

        public async Task<List<CommentData>> GetCommentsByMovieId(string movieId)
        {
            try
            {
                // Retrieve the movie from the database
                var movie = await GetMovieByMovieId(movieId);

                if (movie == null)
                {
                    _logger.LogError("Movie with MovieId: {movieId} not found.", movieId);
                    return new List<CommentData>();
                }

                // Return the list of CommentData objects
                return movie.Comments;
            }
            catch (AmazonDynamoDBException e)
            {
                _logger.LogError("Failed to retrieve comments for MovieId: {movieId}. Exception: {ErrorMessage}", movieId, e.Message);
                return new List<CommentData>();
            }
        }


    }
}
