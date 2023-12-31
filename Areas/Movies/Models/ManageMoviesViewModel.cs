﻿namespace ratingsflex.Areas.Movies.Models
{
    public class ManageMoviesViewModel
    {
        public List<MovieItem> Movies { get; set; }
        public PaginationModel Pagination { get; set; }
        public string SelectedGenre { get; set; }

        public string SelectedRating { get; set; }
    }
}
