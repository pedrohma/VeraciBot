using VeraciBot.Entities;
using VeraciLib.Helper;
using VeraciLib.Interfaces.Infra;
using VeraciLib.Interfaces.Infra.Base;

namespace VeraciInfra.Services.Data;

public partial class TweetService : ITweetService
{
    private readonly IRepository<TweetAuthor> _tweetAuthorRepository;

    public TweetService(IRepository<TweetAuthor> tweetAuthorRepository)
    {
        ArgumentNullException.ThrowIfNull(tweetAuthorRepository);
        _tweetAuthorRepository = tweetAuthorRepository;
    }

    public async Task ComputeAuthors(string authorId, string originalAuthorId, int result = 0)
    {
        var author = await GetTweetAuthor(authorId);
        var originalAuthor = await GetTweetAuthor(originalAuthorId);

        switch (result)
        {
            case 1:
                author.Value += 4;
                originalAuthor.Value -= 5;
                break;
            case 2:
                author.Value += 1;
                originalAuthor.Value -= 2;
                break;
            case 3:
                author.Value -= 1;
                break;
            case 4:
                author.Value -= 3;
                originalAuthor.Value += 2;
                break;
            case 5:
                author.Value -= 6;
                originalAuthor.Value += 5;
                break;
        }

        await _tweetAuthorRepository.UpdateAsync(author);
        await _tweetAuthorRepository.UpdateAsync(originalAuthor);
    }

    public Task<string> GetFullScoreBoard(int top = 10)
    {
        return Task.FromResult(string.Empty); // needs to be implemented
    }

    public async Task<TweetAuthor> GetTweetAuthor(string id, string username = "", string name = "")
    {
        TweetAuthor author = await _tweetAuthorRepository.GetByIdAsync(id);

        if (author is null)
        {
            author = new TweetAuthor()
            {
                Id = id,
                UserName = username,
                Name = name,
                Value = 100,
            };
            await _tweetAuthorRepository.AddAsync(author);
            return author;
        }

        bool changed = false;

        if (StringHelper.SetIfChanged(author.UserName, username, out var newUsername))
        {
            author.UserName = newUsername;
            changed = true;
        }

        if (StringHelper.SetIfChanged(author.Name, name, out var newName))
        {
            author.Name = newName;
            changed = true;
        }

        if (changed)
        {
            await _tweetAuthorRepository.UpdateAsync(author);
        }

        return author;
    }
}
