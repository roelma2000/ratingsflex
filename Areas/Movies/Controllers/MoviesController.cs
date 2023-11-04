using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ratingsflex.Areas.Movies.Data;
using ratingsflex.Areas.Movies.Models;
using System;
using Microsoft.AspNetCore.Http;
using ratingsflex.Areas.Identity.Data;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Net;

namespace ratingsflex.Areas.Movies.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IDynamoDbService _dynamoDbService;
        private readonly ILogger<MoviesController> _logger;
        private readonly IS3Service _s3Service;
        private readonly ApplicationDbContext _context;

        private const string S3MovieBucket = "ratingsflexmovies";
        private const string S3PosterBucket = "ratingsflexposters";
        private const string MovieURL = $"https://{S3MovieBucket}.s3.ca-central-1.amazonaws.com";
        private const string PosterURL = $"https://{S3PosterBucket}.s3.ca-central-1.amazonaws.com";
        private const string DefaultMovie = "time.mp4";
        private const string DefaultPoster = "defaultposter.png";

        public MoviesController(ApplicationDbContext context, IDynamoDbService dynamoDbService, ILogger<MoviesController> logger, IS3Service s3Service)
        {
            _dynamoDbService = dynamoDbService;
            _logger = logger;
            _s3Service = s3Service;
            _context = context;
        }

        public async Task<IActionResult> ManageMovies(string selectedGenre = "All", string selectedRating = "0", int page = 1, int pageSize = 10)
        {
            var (movies, totalItems) = await _dynamoDbService.GetMoviesByUploaderUserId(User.Identity.Name, selectedGenre, selectedRating, page, pageSize);
            var viewModel = new ManageMoviesViewModel
            {
                Movies = movies,
                Pagination = new PaginationModel
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                },
                SelectedGenre = selectedGenre, // Pass the selected genre to the view
                SelectedRating = selectedRating// Pass the selected genre to the view
            };

            return View("~/Areas/Movies/Views/ManageMovies.cshtml", viewModel);
        }

        public async Task<IActionResult> BrowseMovies(string selectedGenre = "All", string selectedRating = "0", int page = 1, int pageSize = 10)
        {
            var (movies, totalItems) = await _dynamoDbService.GetAllMovies(selectedGenre, selectedRating, page, pageSize);
            var viewModel = new BrowseMovieModel
            {
                Movies = movies,
                Pagination = new PaginationModel
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                },
                SelectedGenre = selectedGenre,
                SelectedRating = selectedRating// Pass the selected genre to the view
            };

            return View("~/Areas/Movies/Views/BrowseMovies.cshtml", viewModel);
        }


        [HttpGet]
        [Route("Movies/GetAvailableMovies")]
        public IActionResult GetAvailableMovies()
        {
            string currentUserEmail = User.Identity.Name;

            var movies = _context.Movies
                        .Where(m => m != null && !m.IsAssigned && m.FileOwner == currentUserEmail)
                        .Select(m => new
                        {
                            FileTitle = m.FileTitle ?? string.Empty,
                            FileName = m.FileName ?? string.Empty
                        })
                        .ToList();

            return Json(movies);
        }

        [HttpGet]
        [Route("Movies/GetAvailablePosters")]
        public IActionResult GetAvailablePosters()
        {
            string currentUserEmail = User.Identity.Name;

            var posters = _context.Posters
                        .Where(m => m != null && !m.IsAssigned && m.FileOwner == currentUserEmail)
                        .Select(m => new
                        {
                            FileTitle = m.FileTitle ?? string.Empty,
                            FileName = m.FileName ?? string.Empty
                        })
                        .ToList();

            return Json(posters);
        }

        public IActionResult AddMovie()
        {
            return View("~/Areas/Movies/Views/AddMovie.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> AddMovie(AddMovieViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogInformation("==================================================================================");
                _logger.LogError("Validation errors: {Errors}", string.Join(", ", errors));
                _logger.LogInformation("There was an error with your submission. Please check the form and try again.");
                _logger.LogInformation("==================================================================================");
                return View("~/Areas/Movies/Views/AddMovie.cshtml", model);
            }

            var MovieFile = model.MovieFile;
            var PosterFile = model.PosterFile;
            var movieId = Guid.NewGuid().ToString();
            //default value of movie and poster if did not choose from dropdown
            if (string.IsNullOrEmpty(model.MovieFile))
            {
                MovieFile = $"{DefaultMovie}";
            }
            if (string.IsNullOrEmpty(model.PosterFile))
            {
                PosterFile = $"{DefaultPoster}";
            }

            // Create a new MovieItem object and populate it with the form values
            var movie = new MovieItem
            {
                MovieId = movieId,
                Title = model.Title,
                Description = model.Description,
                ReleaseTime = model.ReleaseTime,
                Genre = model.Genre,
                MoviePath = $"{MovieURL}/{MovieFile}",
                PosterPath = $"{PosterURL}/{PosterFile}",
                UploaderUserId = User.Identity.Name, // uploader's user ID from the logged-in user
                Actors = model.Actors?.ToList() ?? new List<string>(),
                Directors = model.Directors?.ToList() ?? new List<string>(),
                Rating = 0.0, // Initialize rating to 0
                UserRatings = new Dictionary<string, double>(), // Initialize user ratings to empty dictionary
                Comments = new List<CommentData>() // Initialize comments 
            };

            // Save movie record to DynamoDB
            try
            {
                await _dynamoDbService.AddMovie(movie);
                TempData["Notification"] = $"Successfully added the movie record for {movie.Title}.";

                // Update SQL Server tables
                var movieRecord = _context.Movies.FirstOrDefault(m => m.FileName == MovieFile);
                if (movieRecord != null)
                {
                    movieRecord.DynamoDBId = movieId;
                    movieRecord.IsAssigned = true;
                    _context.SaveChanges();
                }

                var posterRecord = _context.Posters.FirstOrDefault(p => p.FileName == PosterFile);
                if (posterRecord != null)
                {
                    posterRecord.DynamoDBId = movieId;
                    posterRecord.IsAssigned = true;
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding movie {MovieTitle}: {ErrorMessage}", movie.Title, ex.Message);
                TempData["Notification"] = $"Error adding movie {movie.Title}.";
                return RedirectToAction("ManageMovies");
            }


            TempData["Notification"] += " Movie added successfully.";
            return RedirectToAction("ManageMovies");
        }

        //******************** Edit Movie ******************
        public async Task<IActionResult> EditMovie(string movieId)
        {
            var movie = await _dynamoDbService.GetMovieByMovieId(movieId);

            var viewModel = new EditMovieViewModel
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                Description = movie.Description,
                ReleaseTime = movie.ReleaseTime,
                Genre = movie.Genre,
                Actors = movie.Actors,
                Directors = movie.Directors,
                MovieFile = movie.MoviePath,
                PosterFile = movie.PosterPath
            };

            _logger.LogInformation($"Model being sent to view: {JsonConvert.SerializeObject(viewModel)}");
            return View("~/Areas/Movies/Views/EditMovie.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> EditMovie(EditMovieViewModel model)
        {
            // 1. Retrieve the existing movie item from the database
            var existingMovie = await _dynamoDbService.GetMovieByMovieId(model.MovieId);

            if (existingMovie == null)
            {
                TempData["Notification"] = $"Movie {model.Title} not found in DynamoDB.";
                return View("~/Areas/Movies/Views/EditMovie.cshtml");
            }

            // 2. Extract the Filename from Movie and Poster Path/URL
            var oldMovieFile = Path.GetFileName(existingMovie.MoviePath);
            var oldPosterFile = Path.GetFileName(existingMovie.PosterPath);

            // 3. Update the properties of the existing movie item with values from the model
            existingMovie.Title = model.Title;
            existingMovie.Description = model.Description;
            existingMovie.ReleaseTime = model.ReleaseTime;
            existingMovie.Genre = model.Genre;
            existingMovie.Actors = model.Actors;
            existingMovie.Directors = model.Directors;

            // 4. Update the existing Movie and Poster Path based on new file
            existingMovie.MoviePath = $"{MovieURL}/{model.MovieFile}";
            existingMovie.PosterPath = $"{PosterURL}/{model.PosterFile}";

            // 5. Save the updated movie item back to the Dynamo database
            await _dynamoDbService.UpdateMovie(existingMovie);

            // 6. Update SQL Server database Files
            //Free-up old files
            var movieRecord = _context.Movies.FirstOrDefault(m => m.FileName == oldMovieFile);
            if (movieRecord != null)
            {
                movieRecord.IsAssigned = false;
                _context.SaveChanges();
            }

            var newMovieRecord = _context.Movies.FirstOrDefault(m => m.FileName == model.MovieFile);
            if (newMovieRecord != null)
            {
                newMovieRecord.IsAssigned = true;
                _context.SaveChanges();
            }

            var posterRecord = _context.Posters.FirstOrDefault(p => p.FileName == oldPosterFile);
            if (posterRecord != null)
            {
                posterRecord.IsAssigned = false;
                _context.SaveChanges();
            }

            var newPosterRecord = _context.Posters.FirstOrDefault(p => p.FileName == model.PosterFile);
            if (newPosterRecord != null)
            {
                newPosterRecord.IsAssigned = true;
                _context.SaveChanges();
            }

            // Redirect to ManageMovies
            return RedirectToAction("ManageMovies");
        }


        //**************************************************
        [HttpPost]
        public async Task<IActionResult> DeleteMovie(string movieId, string releaseTime)
        {
            var success = await _dynamoDbService.DeleteMovieByMovieId(movieId, releaseTime);
            if (success)
            {
                // Update SQL Server tables
                var movieRecords = _context.Movies.Where(m => m.DynamoDBId == movieId);
                foreach (var movieRecord in movieRecords)
                {
                    movieRecord.DynamoDBId = null;
                    movieRecord.IsAssigned = false;
                }

                var posterRecords = _context.Posters.Where(p => p.DynamoDBId == movieId);
                foreach (var posterRecord in posterRecords)
                {
                    posterRecord.DynamoDBId = null;
                    posterRecord.IsAssigned = false;
                }

                await _context.SaveChangesAsync();
                return new JsonResult(new { success = true, message = $"Successfully deleted the movie {movieId}." });
            }
            else
            {
                return new JsonResult(new { success = false, message = "Failed to delete movie. Please try again." });
            }
        }

    
        public IActionResult UploadMovie()
        {
            _logger.LogInformation("UploadMovie method called");
            return View("~/Areas/Movies/Views/Upload.cshtml");
        }

        [HttpPost]
        public async Task<IActionResult> Upload(UploadViewModel model)
        {
            _logger.LogInformation("Upload method called");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogInformation("==================================================================================");
                _logger.LogError("Validation errors: {Errors}", string.Join(", ", errors));
                _logger.LogInformation("There was an error with your submission. Please check the form and try again.");
                _logger.LogInformation("==================================================================================");
                return View("~/Areas/Movies/Views/Upload.cshtml", model);
            }

            try
            {
                var movieFileName = await _s3Service.UploadFileAsync(model.MovieFile, "ratingsflexmovies");
                var posterFileName = await _s3Service.UploadFileAsync(model.PosterFile, "ratingsflexposters");

                var movie = new Movie
                {
                    DynamoDBId = "",
                    FileTitle = model.MovieTitle,
                    FileName = movieFileName,
                    IsAssigned = false,
                    FileOwner = User.Identity.Name
                };

                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();  // Save changes to generate the movie ID

                var poster = new Poster
                {
                    MovieId = movie.Id,  // Set the MovieId property to the Id of the movie
                    DynamoDBId = "",
                    FileTitle = model.MovieTitle,
                    FileName = posterFileName,
                    IsAssigned = false,
                    FileOwner = User.Identity.Name
                };

                _context.Posters.Add(poster);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Movie and poster uploaded successfully!";
                return RedirectToAction("ManageMovies");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload movie and poster");
                TempData["ErrorMessage"] = "Failed to upload movie and poster. Please try again later.";
                return View("~/Areas/Movies/Views/Upload.cshtml");
            }
        }

        public async Task<IActionResult> ManageFiles()
        {
            _logger.LogInformation("\nManageFiles method called\n");
            var movies = _context.Movies
                .Select(m => new ManageUploadedFile
                {
                    Id = m.Id,
                    FileTitle = m.FileTitle,
                    IsAssigned = m.IsAssigned
                })
                .ToList();

            var viewModel = new ManageUploadedFilesViewModel
            {
                UploadedFiles = movies
            };

            return View("~/Areas/Movies/Views/ManageFiles.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMovieAndPosterFile(int id)
        {
            _logger.LogInformation("\nDeleteMovieAndPosterFile method called with id: {Id}\n", id);
            // Find the movie and poster by id
            var movie = _context.Movies.FirstOrDefault(m => m.Id == id);
            var poster = _context.Posters.FirstOrDefault(p => p.MovieId == id);

            if (movie == null || poster == null)
            {
                TempData["ErrorMessage"] = "Movie or poster not found.";
                return RedirectToAction("ManageFiles");
            }

            // Delete the movie and poster files from S3 buckets
            await _s3Service.DeleteFileAsync(movie.FileName, "ratingsflexmovies");
            await _s3Service.DeleteFileAsync(poster.FileName, "ratingsflexposters");

            // Delete the movie and poster records from the database
            _context.Movies.Remove(movie);
            _context.Posters.Remove(poster);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Movie and poster deleted successfully!";
            return RedirectToAction("ManageFiles");
        }

        public async Task<IActionResult> DisplayMovie(string movieId)
        {
            var movieItem = await _dynamoDbService.GetMovieDetails(movieId);
            if (movieItem == null)
            {
                return NotFound();
            }

            var displayMovieViewModel = new DisplayMovieViewModel
            {
                MovieId = movieItem.MovieId,
                Title = movieItem.Title,
                Description = movieItem.Description,
                Actors = movieItem.Actors,
                Directors = movieItem.Directors,
                ReleaseTime = movieItem.ReleaseTime,
                Genre = movieItem.Genre,
                Rating = movieItem.Rating.ToString(),
                PosterPath = movieItem.PosterPath,
                MoviePath = movieItem.MoviePath,
                UploaderUserId = movieItem.UploaderUserId,
                Comments = movieItem.Comments.Select(c => new CommentItem
                {
                    CommentText = c.CommentText,
                    Timestamp = c.Timestamp,
                    UserId = c.UserId
                }).ToList()
            };

            return View("~/Areas/Movies/Views/DisplayMovie.cshtml", displayMovieViewModel);
        }

        [HttpGet]
        [Route("Movies/DownloadMovie")]
        public async Task<IActionResult> DownloadMovie(string key)
        {
            // Decode the key in case it's URL-encoded
            key = WebUtility.UrlDecode(key);

            // Extract the filename from the key
            var uri = new Uri(key);
            var fileName = Path.GetFileName(uri.LocalPath);

            // Use the constant for the bucket name and the extracted filename
            var stream = await _s3Service.GetFileAsync(fileName, S3MovieBucket);
            if (stream == null)
            {
                return NotFound("The file was not found.");
            }

            var contentType = "application/octet-stream";
            // The 'File' method returns a FileResult object which will prompt the browser to download the file
            return File(stream, contentType, fileName);
        }


        [HttpPost]
        public async Task<ActionResult> UpdateComment(string movieId, string userId, string timestamp, string newCommentText)
        {
            try
            {
                // Retrieve the movie item from DynamoDB using the GetMovieDetails method
                var movieItem = await _dynamoDbService.GetMovieDetails(movieId);

                if (movieItem == null)
                {
                    return Json(new { success = false, message = "Movie not found." });
                }

                // Serialize the movieItem to a JSON string for logging
                //string movieItemJson = JsonConvert.SerializeObject(movieItem, Formatting.Indented);
                //_logger.LogInformation("\nMovieItem details: {MovieItemJson}\n", movieItemJson);

                // Find the comment to update
                var commentToUpdate = movieItem.Comments.FirstOrDefault(c => c.UserId == userId && c.Timestamp == timestamp);
                if (commentToUpdate == null)
                {
                    return Json(new { success = false, message = "Comment not found." });
                }

                // Update the comment text
                commentToUpdate.CommentText = newCommentText;

                // Save the updated movie item back to DynamoDB using the UpdateMovie method
                await _dynamoDbService.UpdateMovie(movieItem);

                // Return a success response
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating comment: {Message}", ex.Message);
                return Json(new { success = false, message = "An error occurred while updating the comment." });
            }
        }


        [HttpPost]
        public async Task<ActionResult> AddComment(string movieId, string commentText)
        {
            if (string.IsNullOrEmpty(commentText))
            {
                return Json(new { success = false, message = "Comment text cannot be empty." });
            }

            try
            {
                // Retrieve the movie item from DynamoDB
                var movieItem = await _dynamoDbService.GetMovieDetails(movieId);
                if (movieItem == null)
                {
                    return Json(new { success = false, message = "Movie not found." });
                }

                // Create a new comment object of type CommentData
                var newComment = new CommentData
                {
                    UserId = User.Identity.Name, // Or another way to identify the user
                    CommentText = commentText,
                    Timestamp = DateTime.UtcNow.ToString("o") // ISO 8601 format
                };

                // Add the new comment to the movie item
                movieItem.Comments.Add(newComment);

                // Save the updated movie item back to DynamoDB
                await _dynamoDbService.UpdateMovie(movieItem);

                // Return the userId and timestamp in the response
                return Json(new { success = true, userId = User.Identity.Name, timestamp = newComment.Timestamp });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error adding comment: {Message}", ex.Message);
                return Json(new { success = false, message = "An error occurred while adding the comment." });
            }
        }

        [HttpPost]
        public async Task<ActionResult> DeleteComment(string movieId, string userId, string timestamp)
        {
            if (string.IsNullOrEmpty(movieId) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(timestamp))
            {
                return Json(new { success = false, message = "Invalid parameters." });
            }

            try
            {
                // Retrieve the movie item from the data store
                var movieItem = await _dynamoDbService.GetMovieDetails(movieId);
                if (movieItem == null)
                {
                    return Json(new { success = false, message = "Movie not found." });
                }

                // Find the comment based on the timestamp and user ID
                var commentToDelete = movieItem.Comments.FirstOrDefault(c => c.UserId == userId && c.Timestamp == timestamp);
                if (commentToDelete == null)
                {
                    return Json(new { success = false, message = "Comment not found." });
                }

                // Check if the comment is less than 24 hours old
                var commentDate = DateTime.Parse(commentToDelete.Timestamp);
                if ((DateTime.UtcNow - commentDate).TotalHours > 24)
                {
                    return Json(new { success = false, message = "Comments can only be deleted within 24 hours of posting." });
                }

                // Remove the comment from the list
                movieItem.Comments.Remove(commentToDelete);

                // Save the updated movie item back to the data store
                await _dynamoDbService.UpdateMovie(movieItem);

                return Json(new { success = true, message = "Comment deleted successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError("Error deleting comment: {Message}", ex.Message);
                return Json(new { success = false, message = "An error occurred while deleting the comment." });
            }
        }


        [HttpPost]
        [Route("Movies/UpdateRating")]
        public async Task<IActionResult> UpdateUserRatingAndOverallRating(string movieId, string userId, int userRating)
        {
            _logger.LogInformation("\nReceived rating: {Rating} for movie: {MovieId} by user: {UserId}\n", userRating, movieId, userId);
            try
            {
                // Retrieve the current movie item
                var movieItem = await _dynamoDbService.GetMovieDetails(movieId);
                if (movieItem == null)
                {
                    // Handle the case where the movie does not exist
                    return Json(new { success = false, message = "Movie does not exist." });
                }

                // Check if the userId already exists in userRatings and update or add the rating
                if (movieItem.UserRatings.ContainsKey(userId))
                {
                    // Update the existing rating
                    movieItem.UserRatings[userId] = userRating;
                }
                else
                {
                    // Add a new rating record
                    movieItem.UserRatings.Add(userId, userRating);
                }

                // Calculate the new overall rating
                var totalRating = movieItem.UserRatings.Values.Sum();
                var numberOfRatings = movieItem.UserRatings.Count;
                var overallRating = (double)totalRating / numberOfRatings;

                // Update the overall rating field
                movieItem.Rating = Math.Round(overallRating, 1); // Assuming Rating is a double. Round to 1 decimal place or as needed.

                // Save the updated movie item back to DynamoDB
                await _dynamoDbService.UpdateMovie(movieItem);
                //return true;
                return Json(new { success = true, userRating = userRating, newTotalRating = movieItem.Rating });
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError("Error updating user rating and overall rating: {Message}", ex.Message);
                return Json(new { success = false, message = "Error updating user rating and overall rating: {Message}", ex.Message });
            }
        }

    }
}

