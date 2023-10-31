using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ratingsflex.Areas.Movies.Models
{
    public class Movie
    {
        public int Id { get; set; } 

        public string? DynamoDBId { get; set; }

        public string? FileTitle { get; set; }

        [Required]
        public string? FileName { get; set; }

        public bool IsAssigned { get; set; }

        public string? FileOwner { get; set; }

        public ICollection<Poster> Posters { get; set; } = new List<Poster>();  // navigation property for posters
    }
}
