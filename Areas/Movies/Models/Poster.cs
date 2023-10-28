using System.ComponentModel.DataAnnotations;

namespace ratingsflex.Areas.Movies.Models
{
    public class Poster
    {
        public int Id { get; set; }

        public string? DynamoDBId { get; set; }

        public string FileTitle { get; set; }

        [Required]
        public string FileName { get; set; }

        public bool IsAssigned { get; set; }
    }
}
