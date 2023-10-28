using System.ComponentModel.DataAnnotations;
using ratingsflex.Areas.Movies.Models;

namespace ratingsflex.Areas.Movies.Models
{
    public class AddMovieViewModel
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string ReleaseTime { get; set; }

        [Required]
        public string Genre { get; set; }
        public List<string> Actors { get; set; }
        public List<string> Directors { get; set; }
        public string? MovieFile { get; set; }
        public string? PosterFile { get; set; }
    }

}
