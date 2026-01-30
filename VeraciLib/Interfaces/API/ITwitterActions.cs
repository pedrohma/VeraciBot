using System;
using VeraciLib.Models;
using VeraciLib.Models.TwitterApi;

namespace VeraciLib.Interfaces.API;

public interface ITwitterActions
{
    Task<UserMentions?> GetUserMentions(string startTime = "");
    Task<TwitterUser?> GetTwitterUserById(string userId);
    Task<TwitterUser?> GetTwitterUserByUserName(string userName);
    Task<string?> GetUsernameById(string userId);
    Task<string?> GetNameById(string id);
    Task<string?> GetRepliedTweetText(string tweetId);
    Task<TweetContext?> GetTweetContext(string tweetId);
    Task<string?> GetTweetTextById(string tweetId);
    Task PostReplyAsync(string message, string replyToTweetId);
    Task PostReplyWithImageAsync(string message, string image, string replyToTweetId);
    Task<ThreadContext?> GetThreadContext(string tweetId, string authorId);
}
