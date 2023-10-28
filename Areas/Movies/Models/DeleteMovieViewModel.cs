using System.ComponentModel.DataAnnotations;

namespace ratingsflex.Areas.Movies.Models
{
    public class DeleteMovieViewModel
    {
        [Required]
        public string MovieId { get; set; }
    }
}
