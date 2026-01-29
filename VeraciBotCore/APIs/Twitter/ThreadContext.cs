using System;

namespace VeraciBotCore.APIs.Twitter;

/// <summary>
/// Descreve toda uma thread
/// </summary>
public class ThreadContext
{
    public string Id { get; set; } = string.Empty; // Id do primeiro tweet da thread será usado para identificar a thread

    public string AuthorA { get; set; } = string.Empty; // Id do autor da thread (primeiro usuário da thread que será o usuário A)

    public string AuthorB { get; set; } = string.Empty; // Id de quem responde a thread (segundo usuário da thread que será o usuário B)

    public List<TweetContext> Tweets { get; set; } = new List<TweetContext>(); // Lista de tweets da thread

    /// <summary>
    /// Pega a descrição da thread
    /// </summary>
    /// <returns></returns>
    public string GetFullDialog()
    {
        string description = "";
        foreach (var tweet in Tweets)
        {
            description += $"{tweet.AuthorUsername}: {tweet.Text}\n";
        }

        return description;
    }

    /// <summary>
    /// Pega inicio da thread para author a
    /// </summary>
    /// <returns></returns>
    public string GetStartA()
    {
        string description = "";
        foreach (var tweet in Tweets)
        {
            if (tweet.AuthorId == AuthorA && tweet.Text != "")
            {
                description = tweet.Text;
                break;
            }
        }

        return description;
    }

    /// <summary>
    /// Pega inicio da thread para author b
    /// </summary>
    /// <returns></returns>
    public string GetStartB()
    {
        string description = "";
        foreach (var tweet in Tweets)
        {
            if (tweet.AuthorId == AuthorB && tweet.Text != "")
            {
                description = tweet.Text;
                break;
            }
        }

        return description;
    }
}
