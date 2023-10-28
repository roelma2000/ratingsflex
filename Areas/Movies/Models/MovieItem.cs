using Amazon.DynamoDBv2.DataModel;
using System.Collections.Generic;

namespace ratingsflex.Areas.Movies.Models
{
    [DynamoDBTable("Movies")]
    public class MovieItem
    {
        [DynamoDBHashKey("movieId")]
        public string MovieId { get; set; }

        [DynamoDBProperty("title")]
        public string Title { get; set; }

        [DynamoDBProperty("description")]
        public string Description { get; set; }

        [DynamoDBRangeKey("releaseTime")]
        public string ReleaseTime { get; set; }

        [DynamoDBProperty("actors")]
        public List<string> Actors { get; set; }

        [DynamoDBProperty("director")]
        public List<string> Directors { get; set; }

        [DynamoDBProperty("genre")]
        public string Genre { get; set; }

        [DynamoDBProperty("moviePath")]
        public string MoviePath { get; set; }

        [DynamoDBProperty("posterPath")]
        public string PosterPath { get; set; }

        [DynamoDBProperty("rating")]
        public string Rating { get; set; }

        [DynamoDBProperty("comments")]
        public List<Dictionary<string, string>> Comments { get; set; }

        [DynamoDBProperty("uploaderUserId")]
        public string UploaderUserId { get; set; }
    }
}
