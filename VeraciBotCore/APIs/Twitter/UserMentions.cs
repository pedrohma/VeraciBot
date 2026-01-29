using System;

namespace VeraciBotCore.APIs.Twitter;

public class UserMentions
{
    public List<Tweet> Tweets { get; set; } = new List<Tweet>();

    public class Tweet
    {
        public string? Id { get; set; }
        public string? Text { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string? CreatedAt { get; set; }
        public List<Entities> Entities { get; set; } = new List<Entities>();
    }

    public class Entities
    {
        public List<UserMention> UserMentions { get; set; } = new List<UserMention>();
    }

    public class UserMention
    {
        public string? Username { get; set; }
        public string? Id { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
    }
}
