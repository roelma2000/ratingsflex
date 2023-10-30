using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ratingsflex.Areas.Movies.Data;
using ratingsflex.Areas.Movies.Models;
using System;
using Microsoft.AspNetCore.Http;
using ratingsflex.Areas.Identity.Data;
using System.Diagnostics;

namespace ratingsflex.Areas.Movies.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IDynamoDbService _dynamoDbService;
        private readonly ILogger<MoviesController> _logger;
        private readonly IS3Service _s3Service;
        private readonly ApplicationDbContext _context;


        public MoviesController(ApplicationDbContext context, IDynamoDbService dynamoDbService, ILogger<MoviesController> logger, IS3Service s3Service)
        {
            _dynamoDbService = dynamoDbService;
            _logger = logger;
            _s3Service = s3Service;
            _context = context;
        }

        public async Task<IActionResult> ManageMovies(int page = 1, int pageSize = 10)
        {
            var (movies, totalItems) = await _dynamoDbService.GetMoviesByUploaderUserId(User.Identity.Name, page, pageSize);
            var viewModel = new ManageMoviesViewModel
            {
                Movies = movies,
                Pagination = new PaginationModel
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                }
            };

            return View("~/Areas/Movies/Views/ManageMovies.cshtml", viewModel);

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
                _logger.LogError("Validation errors: " + string.Join(", ", errors));
                _logger.LogInformation("There was an error with your submission. Please check the form and try again.");
                _logger.LogInformation("==================================================================================");
                return View("~/Areas/Movies/Views/AddMovie.cshtml", model);
            }
            var S3MovieBucket = "ratingsflexmovies";
            var S3PosterBucket = "ratingsflexposters";
            var MovieURL = $"https://{S3MovieBucket}.s3.ca-central-1.amazonaws.com";
            var PosterURL = $"https://{S3PosterBucket}.s3.ca-central-1.amazonaws.com";
            var DefaultMovie = "time.mp4";
            var DefaultPoster = "defaultposter.png";

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
                Rating = "0", // Initialize rating to 0
                Comments = new List<Dictionary<string, string>>() // Initialize comments to empty list
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
                    _context.Movies.Remove(movieRecord);
                    _context.SaveChanges();

                    movieRecord.DynamoDBId = movieId;
                    movieRecord.IsAssigned = true;

                    _context.Movies.Add(movieRecord);
                    _context.SaveChanges();
                }

                var posterRecord = _context.Posters.FirstOrDefault(p => p.FileName == PosterFile);
                if (posterRecord != null)
                {
                    _context.Posters.Remove(posterRecord);
                    _context.SaveChanges();

                    posterRecord.DynamoDBId = movieId;
                    posterRecord.IsAssigned = true;

                    _context.Posters.Add(posterRecord);
                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding movie {movie.Title}: {ex.Message}");
                TempData["Notification"] = $"Error adding movie {movie.Title}.";
                return RedirectToAction("ManageMovies");
            }


            TempData["Notification"] += " Movie added successfully.";
            return RedirectToAction("ManageMovies");
        }

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

        [HttpPost]
        public async Task<IActionResult> AddComment(string movieId, string commentText)
        {
            if (string.IsNullOrEmpty(commentText))
            {
                return new JsonResult(new { success = false, message = "Comment text cannot be empty." });
            }

            var movie = await _dynamoDbService.GetMovieByMovieId(movieId);
            if (movie == null)
            {
                return new JsonResult(new { success = false, message = "Movie not found." });
            }

            var comment = new Dictionary<string, string>
    {
        { "commentText", commentText },
        { "userId", User.Identity.Name },
        { "timestamp", DateTime.UtcNow.ToString("o") }
    };

            movie.Comments.Add(comment);

            try
            {
                await _dynamoDbService.UpdateMovie(movie);
                return new JsonResult(new { success = true, message = "Comment added successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding comment to movie {movie.Title}: {ex.Message}");
                return new JsonResult(new { success = false, message = "Failed to add comment. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditComment(string movieId, int commentIndex, string newCommentText)
        {
            if (string.IsNullOrEmpty(newCommentText))
            {
                return new JsonResult(new { success = false, message = "Comment text cannot be empty." });
            }

            var movie = await _dynamoDbService.GetMovieByMovieId(movieId);
            if (movie == null)
            {
                return new JsonResult(new { success = false, message = "Movie not found." });
            }

            if (commentIndex < 0 || commentIndex >= movie.Comments.Count)
            {
                return new JsonResult(new { success = false, message = "Invalid comment index." });
            }

            var comment = movie.Comments[commentIndex];
            var userId = comment["userId"];
            var timestamp = DateTime.Parse(comment["timestamp"]);

            if (User.Identity.Name != userId)
            {
                return new JsonResult(new { success = false, message = "You can only edit your own comments." });
            }

            if ((DateTime.UtcNow - timestamp).TotalHours > 24)
            {
                return new JsonResult(new { success = false, message = "You can only edit comments within 24 hours of posting." });
            }

            comment["commentText"] = newCommentText;
            comment["timestamp"] = DateTime.UtcNow.ToString("o");

            try
            {
                await _dynamoDbService.UpdateMovie(movie);
                return new JsonResult(new { success = true, message = "Comment edited successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error editing comment on movie {movie.Title}: {ex.Message}");
                return new JsonResult(new { success = false, message = "Failed to edit comment. Please try again." });
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
                _logger.LogError("Validation errors: " + string.Join(", ", errors));
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
                    DynamoDBId = Guid.NewGuid().ToString(),
                    FileTitle = model.MovieTitle,
                    FileName = movieFileName,
                    IsAssigned = false,
                    FileOwner = User.Identity.Name
                };

                var poster = new Poster
                {
                    DynamoDBId = Guid.NewGuid().ToString(),
                    FileTitle = model.MovieTitle,
                    FileName = posterFileName,
                    IsAssigned = false,
                    FileOwner = User.Identity.Name
                };

                _context.Movies.Add(movie);
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



    }
}

