namespace Assignment3AddieZiegler.Models
{
    public class RedditPostVM
    {
        public Actor Actor { get; set; }
        public Movie Movie { get; set; }
        public string Sentiment { get; set; }
        public List<RedditPost> RedditResults { get; set; }
    }
}
