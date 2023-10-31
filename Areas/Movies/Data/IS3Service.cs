using System.Collections.Generic;
using System.Threading.Tasks;

namespace ratingsflex.Areas.Movies.Data
{
    public interface IS3Service
    {

        Task<string> UploadFileAsync(IFormFile file, string bucketName);
        Task DeleteFileAsync(string fileKey, string bucketName);
    }
}
