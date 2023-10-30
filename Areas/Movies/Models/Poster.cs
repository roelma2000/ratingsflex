using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ratingsflex.Areas.Movies.Models
{
    public class Poster
    {
        public string? DynamoDBId { get; set; }

        public string? FileTitle { get; set; }

        [Required]
        public string? FileName { get; set; }

        public bool IsAssigned { get; set; }

        public string? FileOwner { get; set; }
    }
}
