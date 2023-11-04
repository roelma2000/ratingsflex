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
        public double Rating { get; set; }

        [DynamoDBProperty("userRatings")]
        public Dictionary<string, double> UserRatings { get; set; }

        [DynamoDBProperty("comments")]
        public List<CommentData> Comments { get; set; }


        [DynamoDBProperty("uploaderUserId")]
        public string UploaderUserId { get; set; }
    }

    public class CommentData
    {
        [DynamoDBProperty("commentText")]
        public string CommentText { get; set; }

        [DynamoDBProperty("timestamp")]
        public string Timestamp { get; set; }

        [DynamoDBProperty("userId")]
        public string UserId { get; set; }
    }

}
