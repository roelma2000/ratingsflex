namespace ratingsflex.Areas.Movies.Models
{
    public class BrowseMovieModel
    {
        public List<ListAllMoviesViewModel> Movies { get; set; }
        public PaginationModel Pagination { get; set; }
        public string SelectedGenre { get; set; }

        public string SelectedRating { get; set; }
    }

}
