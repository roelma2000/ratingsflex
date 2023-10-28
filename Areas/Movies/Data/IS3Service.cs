using System.Collections.Generic;
using System.Threading.Tasks;

namespace ratingsflex.Areas.Movies.Data
{
    public interface IS3Service
    {
        Task<List<string>> GetAvailableMovies();
        Task<List<string>> GetAvailablePosters();
    }
}
