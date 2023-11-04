namespace ratingsflex.Areas.Movies.Models
{
    public class DisplayMovieViewModel
    {
        public string MovieId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<string> Actors { get; set; }
        public List<string> Directors { get; set; }
        public string ReleaseTime { get; set; }
        public string Genre { get; set; }
        public string Rating { get; set; }
        public string PosterPath { get; set; }
        public string MoviePath { get; set; }
        public string UploaderUserId { get; set; }
        public List<CommentItem> Comments { get; set; }
    }

    public class CommentItem
    {
        public string CommentText { get; set; }
        public string Timestamp { get; set; }
        public string UserId { get; set; }
    }

}
