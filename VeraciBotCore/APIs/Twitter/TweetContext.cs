using System;

namespace VeraciBotCore.APIs.Twitter;

/// <summary>
/// Descreve um tweet específico
/// </summary>
public class TweetContext
{
    public string Id { get; set; } = string.Empty; // Id do tweet
    public string AuthorId { get; set; } = string.Empty; //Id do autor do tweet
    public string AuthorName { get; set; } = string.Empty; // Nome do author do tweet
    public string AuthorUsername { get; set; } = string.Empty; // Nome de usuário do author do tweet (@)
    public string Text { get; set; } = string.Empty; // Texto do tweet
    public string CreatedAt { get; set; } = string.Empty; // Data da criação
    public string RepliedToId { get; set; } = string.Empty; // É resposta a tweet
}
