using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ratingsflex.Areas.Movies.Models
{
    public class UploadViewModel
    {
        [Required]
        [Display(Name = "Movie Title")]
        public string MovieTitle { get; set; }

        [Required]
        [Display(Name = "Movie File")]
        public IFormFile MovieFile { get; set; }

        [Required]
        [Display(Name = "Poster File")]
        public IFormFile PosterFile { get; set; }
    }
}
