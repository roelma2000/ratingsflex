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
            var movies = _context.Movies
                        .Where(m => m != null && !m.IsAssigned)
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
            var posters = _context.Posters
                        .Where(m => m != null && !m.IsAssigned)
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
                    movieRecord.DynamoDBId = movieId;
                    movieRecord.IsAssigned = true;
                }

                var posterRecord = _context.Posters.FirstOrDefault(p => p.FileName == PosterFile);
                if (posterRecord != null)
                {
                    posterRecord.DynamoDBId = movieId;
                    posterRecord.IsAssigned = true;
                }

                _context.SaveChanges();
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



    }
}

