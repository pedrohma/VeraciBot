using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using VeraciBot.Entities;
using VeraciInfra.Services.API;
using VeraciLib.Interfaces.API;
using VeraciLib.Interfaces.Infra;
using VeraciLib.Interfaces.Infra.Base;
using VeraciLib.Models.OpenAiApi;
using VeraciLib.Models.TwitterApi;
using VeraciLib.Settings;

namespace VeraciBot.Handlers;

public class TwitterBotHandler
{
    private readonly IRepository<Tweet> _tweetRepo;
    private readonly IRepository<AuthorizedUser> _authRepo;
    private readonly ITwitterActions _twitterApi;
    private readonly IOpenAiActions _openAi;
    private readonly IConfigService _configService;
    private readonly ITweetService _tweetService;
    private readonly Phrases _phrases;
    private readonly ILogger<TwitterBotHandler> _logger;

    public TwitterBotHandler(
        IRepository<Tweet> tweetRepo,
        IRepository<AuthorizedUser> authRepo,
        ITwitterActions twitterApi,
        IOpenAiActions openAi,
        IConfigService configService,
        ITweetService tweetService,
        Phrases phrases,
        ILogger<TwitterBotHandler> logger
    )
    {
        ArgumentNullException.ThrowIfNull(tweetRepo);
        ArgumentNullException.ThrowIfNull(authRepo);
        ArgumentNullException.ThrowIfNull(twitterApi);
        ArgumentNullException.ThrowIfNull(openAi);
        ArgumentNullException.ThrowIfNull(configService);
        ArgumentNullException.ThrowIfNull(tweetService);
        ArgumentNullException.ThrowIfNull(phrases);
        ArgumentNullException.ThrowIfNull(logger);
        _tweetRepo = tweetRepo;
        _authRepo = authRepo;
        _openAi = openAi;
        _configService = configService;
        _tweetService = tweetService;
        _phrases = phrases;
        _logger = logger;
        _twitterApi = twitterApi;
    }

    public async Task ProcessMentionsAsync(CancellationToken ct)
    {
        var startTime = await _configService.GetLastDateTimeForTwitterCheck();
        _logger.LogInformation($"Fetching mentions since {startTime}");
        var mentions = await _twitterApi.GetUserMentions(startTime.ToString());

        if (mentions?.Tweets == null)
        {
            _logger.LogInformation("No mentions retrieved from Twitter API");
            return;
        }

        foreach (var tweet in mentions.Tweets)
        {
            await ProcessTweetAsync(tweet);
        }
    }

    private async Task ProcessTweetAsync(UserMentions.Tweet tweet)
    {
        if (await IsAlreadyProcessed(tweet))
            return;

        if (!await IsAuthorized(tweet))
        {
            await ReplyNotAuthorized(tweet);
            return;
        }

        var command = await IdentifyCommand(tweet);

        if (!IsValidCommand(command))
        {
            await ReplyInvalidCommand(tweet);
            return;
        }

        await ExecuteCommand(tweet, command);
    }

    private async Task<bool> IsAlreadyProcessed(UserMentions.Tweet tweet)
    {
        if (tweet.Id == null)
            return true;

        var existing = await _tweetRepo.FindOneAsync(t => t.Id == tweet.Id);
        return existing != null;
    }

    private async Task<bool> IsAuthorized(UserMentions.Tweet tweet)
    {
        if (tweet.AuthorId == null)
            return false;

        var auth = await _authRepo.FindOneAsync(a => a.Id == tweet.AuthorId);

        return auth != null && auth.Status == AuthorizationStatus.Authorized;
    }

    private async Task ReplyNotAuthorized(UserMentions.Tweet tweet)
    {
        _logger.LogInformation($"User {tweet.AuthorId} is not authorized");

        var entity = new Entities.Tweet
        {
            Id = tweet.Id!,
            ThreadId = tweet.Id!,
            AuthorId = tweet.AuthorId!,
            OriginalAuthorId = tweet.AuthorId!,
            Result = 0,
        };

        await _tweetRepo.AddAsync(entity);

        var text = await _phrases.GetNotAuthorizedResponseAsync("pt");

        await _twitterApi.PostReplyWithImageAsync(text, Images.notAuthorizedImage, tweet.Id!);
    }

    private async Task<IdentifiedCommand?> IdentifyCommand(UserMentions.Tweet tweet)
    {
        string commandText = tweet.Text ?? "";

        // single tweet by default
        bool isSingleTweet = true;

        return await _openAi.CheckCommand(commandText, isSingleTweet);
    }

    private static readonly HashSet<int> _validCommands = new()
    {
        OpenAiServices.CMD_HELP,
        OpenAiServices.CMD_SCORE,
        OpenAiServices.CMD_SCOREBOARD,
        OpenAiServices.CMD_INVITE,
        OpenAiServices.CMD_ACCEPT_INVITE,
        OpenAiServices.CMD_REFUSE_INVITE,
        OpenAiServices.CMD_THREAD_FALSE,
        OpenAiServices.CMD_THREAD_ARGUE,
        OpenAiServices.CMD_THREAD_WHOISRIGHT,
    };

    private static bool IsValidCommand(IdentifiedCommand? cmd)
    {
        return cmd != null && _validCommands.Contains(cmd.Result);
    }

    private async Task ReplyInvalidCommand(UserMentions.Tweet tweet)
    {
        _logger.LogInformation($"Invalid command from tweet {tweet.Id}");

        var entity = new Entities.Tweet
        {
            Id = tweet.Id!,
            ThreadId = tweet.Id!,
            AuthorId = tweet.AuthorId!,
            OriginalAuthorId = tweet.AuthorId!,
            Result = 0,
        };

        await _tweetRepo.AddAsync(entity);

        var text = await _phrases.GetFailedToUnderstandResponseAsync("pt");

        await _twitterApi.PostReplyWithImageAsync(text, Images.failedToUnderstandImage, tweet.Id!);
    }

    private async Task ExecuteCommand(UserMentions.Tweet tweet, IdentifiedCommand command)
    {
        switch (command.Result)
        {
            case OpenAiServices.CMD_HELP:
                await HandleHelp(tweet);
                break;

            case OpenAiServices.CMD_SCORE:
                await HandleScore(tweet);
                break;

            case OpenAiServices.CMD_SCOREBOARD:
                await HandleScoreboard(tweet);
                break;

            case OpenAiServices.CMD_INVITE:
                await HandleInvite(tweet);
                break;

            case OpenAiServices.CMD_ACCEPT_INVITE:
                await HandleAcceptInvite(tweet);
                break;

            case OpenAiServices.CMD_REFUSE_INVITE:
                await HandleRefuseInvite(tweet);
                break;

            default:
                _logger.LogInformation($"Command {command.Result} not implemented yet");
                break;
        }
    }

    private async Task HandleHelp(UserMentions.Tweet tweet)
    {
        var entity = new Entities.Tweet
        {
            Id = tweet.Id!,
            ThreadId = tweet.Id!,
            AuthorId = tweet.AuthorId!,
            OriginalAuthorId = tweet.AuthorId!,
            Result = 0,
        };

        await _tweetRepo.AddAsync(entity);

        string text = await _phrases.GetHelpResponseAsync("pt");

        await _twitterApi.PostReplyWithImageAsync(text, Images.helpImage, tweet.Id!);
    }

    private async Task HandleScore(UserMentions.Tweet tweet)
    {
        var entity = new Entities.Tweet
        {
            Id = tweet.Id!,
            ThreadId = tweet.Id!,
            AuthorId = tweet.AuthorId!,
            OriginalAuthorId = tweet.AuthorId!,
            Result = 0,
        };

        await _tweetRepo.AddAsync(entity);

        var twitterUser = await _twitterApi.GetTwitterUserById(tweet.AuthorId!);

        var author = await _tweetService.GetTweetAuthor(
            tweet.AuthorId!,
            twitterUser.Username,
            twitterUser.Name
        );

        string text = await _phrases.GetScoreResponseAsync("pt");
        text += "\r\n\r\n" + author.GetDescription();

        await _twitterApi.PostReplyWithImageAsync(text, Images.scoreImage, tweet.Id!);
    }

    private async Task HandleScoreboard(UserMentions.Tweet tweet)
    {
        var entity = new Entities.Tweet
        {
            Id = tweet.Id!,
            ThreadId = tweet.Id!,
            AuthorId = tweet.AuthorId!,
            OriginalAuthorId = tweet.AuthorId!,
            Result = 0,
        };

        await _tweetRepo.AddAsync(entity);

        string text = await _phrases.GetScoreResponseAsync("pt");
        text += "\r\n\r\n" + _tweetService.GetFullScoreBoard(10);

        await _twitterApi.PostReplyWithImageAsync(text, Images.scoreBoardImage, tweet.Id!);
    }

    private async Task HandleInvite(UserMentions.Tweet tweet)
    {
        var entity = new Entities.Tweet
        {
            Id = tweet.Id!,
            ThreadId = tweet.Id!,
            AuthorId = tweet.AuthorId!,
            OriginalAuthorId = tweet.AuthorId!,
            Result = 0,
        };

        await _tweetRepo.AddAsync(entity);

        string inviteText = await _phrases.GetInviteResponseAsync("pt");

        string commandText = tweet.Text ?? "";
        string[] users = TwitterServices.FindUsersReference(commandText);

        var authorUser = await _twitterApi.GetTwitterUserById(tweet.AuthorId!);
        string authorUsername = authorUser.Username.ToLower();

        string inviteUsername = "";

        for (int i = users.Length - 1; i >= 0; i--)
        {
            var u = users[i].ToLower();
            if (u != "veracibot" && u != authorUsername)
            {
                inviteUsername = users[i];
                break;
            }
        }

        if (string.IsNullOrEmpty(inviteUsername))
        {
            string text = await _phrases.GetInviteNoUserResponseAsync("pt");

            await _twitterApi.PostReplyWithImageAsync(text, Images.inviteNoUserImage, tweet.Id!);
            return;
        }

        var invitedUser = await _twitterApi.GetTwitterUserByUserName(inviteUsername);

        var auth = await _authRepo.FindOneAsync(a => a.Id == invitedUser.Id);

        if (
            auth != null
            && (
                auth.Status == AuthorizationStatus.Authorized
                || auth.Status == AuthorizationStatus.Invited
            )
        )
        {
            string text = await _phrases.GetInviteErrorResponseAsync("pt");

            await _twitterApi.PostReplyWithImageAsync(text, Images.inviteErrorImage, tweet.Id!);
            return;
        }

        if (auth != null)
        {
            auth.Status = AuthorizationStatus.Invited;
            auth.AuthorizationDate = DateTime.UtcNow;
            auth.AuthorizedById = tweet.AuthorId!;
            await _authRepo.UpdateAsync(auth);
        }
        else
        {
            await _authRepo.AddAsync(
                new AuthorizedUser
                {
                    Id = invitedUser.Id,
                    AuthorizedById = tweet.AuthorId!,
                    AuthorizationDate = DateTime.UtcNow,
                    Status = AuthorizationStatus.Invited,
                }
            );
        }

        inviteText = inviteUsername + " " + inviteText;

        await _twitterApi.PostReplyWithImageAsync(inviteText, Images.inviteImage, tweet.Id!);
    }

    private async Task HandleAcceptInvite(UserMentions.Tweet tweet)
    {
        var entity = new Entities.Tweet
        {
            Id = tweet.Id!,
            ThreadId = tweet.Id!,
            AuthorId = tweet.AuthorId!,
            OriginalAuthorId = tweet.AuthorId!,
            Result = 0,
        };

        await _tweetRepo.AddAsync(entity);

        var auth = await _authRepo.FindOneAsync(a => a.Id == tweet.AuthorId!);
        if (auth != null)
        {
            auth.Status = AuthorizationStatus.Authorized;
            await _authRepo.UpdateAsync(auth);
        }

        string text = await _phrases.GetAcceptResponseAsync("pt");

        await _twitterApi.PostReplyWithImageAsync(text, Images.acceptImage, tweet.Id!);
    }

    private async Task HandleRefuseInvite(UserMentions.Tweet tweet)
    {
        var entity = new Entities.Tweet
        {
            Id = tweet.Id!,
            ThreadId = tweet.Id!,
            AuthorId = tweet.AuthorId!,
            OriginalAuthorId = tweet.AuthorId!,
            Result = 0,
        };

        await _tweetRepo.AddAsync(entity);

        var auth = await _authRepo.FindOneAsync(a => a.Id == tweet.AuthorId!);
        if (auth != null)
        {
            auth.Status = AuthorizationStatus.NotAuthorized;
            await _authRepo.UpdateAsync(auth);
        }

        string text = await _phrases.GetRefuseResponseAsync("pt");

        await _twitterApi.PostReplyWithImageAsync(text, Images.refuseImage, tweet.Id!);
    }
}
