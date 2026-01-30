using VeraciBot.Entities;

namespace VeraciLib.Interfaces.Infra;

public partial interface ITweetService
{
    /// <summary>
    /// Computa os valores dos autores do tweet
    /// </summary>
    /// <param name="authorId"></param>
    /// <param name="originalAuthorId"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    Task ComputeAuthors(string authorId, string originalAuthorId, int result = 0);

    /// <summary>
    /// Retorna o autor do tweet
    /// </summary>
    /// <param name="id"></param>
    Task<TweetAuthor> GetTweetAuthor(string id, string username = "", string name = "");

    /// <summary>
    /// Retorna o placar completo
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    Task<string> GetFullScoreBoard(int top = 10);
}
