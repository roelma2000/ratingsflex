namespace ratingsflex.Areas.Movies.Models
{
    public class ListAllMoviesViewModel
    {
        public string MovieId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime ReleaseTime { get; set; }
        public string Genre { get; set; }
        public string MoviePath { get; set; }
        public string PosterPath { get; set; }
        public string UploaderUserId { get; set; }
        public List<string> Actors { get; set; }
        public List<string> Directors { get; set; }
        public double Rating { get; set; }
        public Dictionary<string, double> UserRatings { get; set; }
    }
}
