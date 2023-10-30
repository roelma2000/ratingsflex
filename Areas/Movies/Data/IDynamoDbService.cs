using System.Collections.Generic;
using System.Threading.Tasks;
using ratingsflex.Areas.Movies.Models;

namespace ratingsflex.Areas.Movies.Data
{
    public interface IDynamoDbService
    {
        Task<(List<MovieItem>, int)> GetMoviesByUploaderUserId(string uploaderUserId, int page = 1, int pageSize = 10);

        Task AddMovie(MovieItem movie);
        Task<List<MovieItem>> GetMoviesById(string movieId);
        Task<bool> DeleteMovieByMovieId(string movieId, string releaseTime);

        Task<MovieItem> GetMovieByMovieId(string movieId);
        Task UpdateMovie(MovieItem movie);

    }
}
