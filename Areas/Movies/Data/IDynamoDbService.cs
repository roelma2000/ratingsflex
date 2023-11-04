using System.Collections.Generic;
using System.Threading.Tasks;
using NuGet.ContentModel;
using ratingsflex.Areas.Movies.Models;

namespace ratingsflex.Areas.Movies.Data
{
    public interface IDynamoDbService
    {
        Task<(List<MovieItem>, int)> GetMoviesByUploaderUserId(string uploaderUserId, string genre = null, string rating = "All", int page = 1, int pageSize = 10);

        Task<(List<ListAllMoviesViewModel>, int)> GetAllMovies(string genre = "All", string rating = "All", int page = 1, int pageSize = 10);
        Task AddMovie(MovieItem movie);
        Task<List<MovieItem>> GetMoviesById(string movieId);

        Task<bool> DeleteMovieByMovieId(string movieId, string releaseTime);

        Task<MovieItem> GetMovieByMovieId(string movieId);
        Task UpdateMovie(MovieItem movie);

        Task<MovieItem> GetMovieDetails(string movieId);
        Task<List<CommentData>> GetCommentsByMovieId(string movieId);

    }
}
