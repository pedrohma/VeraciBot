using System;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Tweetinvi;
using VeraciLib.Interfaces.API;
using VeraciLib.Models.TwitterApi;
using VeraciLib.Settings;

namespace VeraciInfra.Services.API;

public class TwitterServices : ITwitterActions
{
    private ILogger<TwitterServices>? _logger = null;
    private readonly HttpClient _httpClient;
    private readonly xSettings _xSettings;

    public TwitterServices(ILogger<TwitterServices> logger, AppSettings settings)
    {
        if (logger is null)
            throw new ArgumentNullException(nameof(logger));
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));
        if (string.IsNullOrEmpty(settings.xSettings.BearerToken))
            throw new ArgumentNullException(nameof(settings.xSettings.BearerToken));
        _logger = logger;
        _xSettings = settings.xSettings;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            settings.xSettings.BearerToken
        );
    }

    /// <summary>
    /// Obtém o nome pelo ID do Twitter
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<string?> GetNameById(string id)
    {
        var user = await GetTwitterUserById(id);
        _logger.LogInformation($"Obtido nome de usuário: {user?.Name}");
        return user?.Name;
    }

    /// <summary>
    /// Obtém o texto do tweet ao qual o tweet especificado respondeu
    /// </summary>
    /// <param name="tweetId"></param>
    /// <returns></returns>
    public async Task<string?> GetRepliedTweetText(string tweetId)
    {
        string url = $"https://api.twitter.com/2/tweets/{tweetId}?tweet.fields=referenced_tweets";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Erro ao acessar a Twitter API: {response.StatusCode}");
            return null;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement.GetProperty("data");

        if (root.TryGetProperty("referenced_tweets", out JsonElement referencedTweets))
        {
            foreach (var refTweet in referencedTweets.EnumerateArray())
            {
                if (refTweet.GetProperty("type").GetString() == "replied_to")
                {
                    string repliedToId = refTweet.GetProperty("id").GetString();
                    return await GetTweetTextById(repliedToId);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Obtém o contexto de um tweet
    /// </summary>
    /// <param name="tweetId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<TweetContext?> GetTweetContext(string tweetId)
    {
        string url =
            $"https://api.twitter.com/2/tweets/{tweetId}?tweet.fields=text,author_id,created_at,referenced_tweets&user.fields=username,name";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Erro ao acessar a Twitter API: {response.StatusCode}");
            return null;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement.GetProperty("data");

        string text = RemoveReferences(root.GetProperty("text").GetString());
        string authorId = root.GetProperty("author_id").GetString();
        string createdAt = root.GetProperty("created_at").GetString();
        string authorName = "";
        string authorUsername = "";
        string repliedToId = "";

        if (
            root.TryGetProperty("includes", out JsonElement includes)
            && includes.TryGetProperty("users", out JsonElement users)
        )
        {
            foreach (var user in users.EnumerateArray())
            {
                if (user.GetProperty("id").GetString() == authorId)
                {
                    authorUsername = user.GetProperty("username").GetString();
                    authorName = user.GetProperty("name").GetString();
                    break;
                }
            }
        }

        if (root.TryGetProperty("referenced_tweets", out JsonElement referencedTweets))
        {
            foreach (var refTweet in referencedTweets.EnumerateArray())
            {
                if (refTweet.GetProperty("type").GetString() == "replied_to")
                {
                    repliedToId = refTweet.GetProperty("id").GetString();
                    break;
                }
            }
        }

        // Aqui tenho todas as informações de um tweet

        return new TweetContext
        {
            Id = tweetId,
            Text = text,
            AuthorId = authorId,
            AuthorName = authorName,
            AuthorUsername = authorUsername,
            CreatedAt = createdAt,
            RepliedToId = repliedToId,
        };
    }

    /// <summary>
    /// Obtém o texto de um tweet pelo ID
    /// </summary>
    /// <param name="tweetId"></param>
    /// <returns></returns>
    public async Task<string?> GetTweetTextById(string tweetId)
    {
        string url = $"https://api.twitter.com/2/tweets/{tweetId}?tweet.fields=text";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Erro ao buscar o tweet original: {response.StatusCode}");
            return null;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement.GetProperty("data");

        return root.GetProperty("text").GetString();
    }

    /// <summary>
    /// Obtém um usuário do Twitter pelo ID
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="Exception"></exception>
    public async Task<TwitterUser?> GetTwitterUserById(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new ArgumentNullException(nameof(userId));
        string url = $"https://api.twitter.com/2/users/{userId}";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Erro ao acessar a Twitter API: {response.StatusCode}");
            throw new Exception($"Erro ao acessar a Twitter API: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var data))
        {
            return new TwitterUser
            {
                Id = data.GetProperty("id").GetString(),
                Name = data.GetProperty("name").GetString(),
                Username = data.GetProperty("username").GetString(),
            };
        }

        return null;
    }

    /// <summary>
    /// Obtém um usuário do Twitter pelo nome de usuário
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    public async Task<TwitterUser?> GetTwitterUserByUserName(string userName)
    {
        if (string.IsNullOrEmpty(userName))
            throw new ArgumentNullException(nameof(userName));
        if (userName.StartsWith("@"))
        {
            userName = userName.Substring(1);
        }
        string url = $"https://api.twitter.com/2/users/by/username/{userName}";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Erro ao acessar a Twitter API: {response.StatusCode}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("data", out var data))
        {
            return new TwitterUser
            {
                Id = data.GetProperty("id").GetString(),
                Name = data.GetProperty("name").GetString(),
                Username = data.GetProperty("username").GetString(),
            };
        }

        return null;
    }

    /// <summary>
    /// Obtém o nome de usuário pelo ID do Twitter
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<string?> GetUsernameById(string userId)
    {
        var user = await GetTwitterUserById(userId);
        return user?.Username;
    }

    /// <summary>
    /// Posta uma resposta a um tweet
    /// </summary>
    /// <param name="message"></param>
    /// <param name="replyToTweetId"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task PostReplyAsync(string message, string replyToTweetId)
    {
        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrEmpty(replyToTweetId))
            throw new ArgumentNullException(nameof(replyToTweetId));

        string url = "https://api.twitter.com/2/tweets";
        string nonce = Convert.ToBase64String(
            new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString())
        );
        string timestamp = Convert
            .ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds)
            .ToString();

        // Parâmetros do corpo (JSON)
        string bodyJson =
            "{\"text\": \""
            + message
            + "\",\"reply\": {\"in_reply_to_tweet_id\": \""
            + replyToTweetId
            + "\"}}";
        var bodyBytes = Encoding.UTF8.GetBytes(bodyJson);

        string authHeader = GenerateOAuthHeader("POST", url);

        var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "OAuth",
            authHeader.Substring(6)
        ); // Remove "OAuth " duplicado
        request.Content = new ByteArrayContent(bodyBytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await _httpClient.SendAsync(request);
        string result = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Resposta do Twitter ao postar reply: {result}");
    }

    /// <summary>
    /// Posta uma resposta a um tweet com imagem
    /// </summary>
    /// <param name="message"></param>
    /// <param name="image"></param>
    /// <param name="replyToTweetId"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public async Task PostReplyWithImageAsync(string message, string image, string replyToTweetId)
    {
        if (string.IsNullOrEmpty(message))
            throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrEmpty(image))
            throw new ArgumentNullException(nameof(image));
        if (string.IsNullOrEmpty(replyToTweetId))
            throw new ArgumentNullException(nameof(replyToTweetId));

        string mediaId = await UploadImageTweetinviAsync(image);
        if (mediaId == null)
        {
            Console.WriteLine("Falha ao enviar imagem.");
            return;
        }

        string url = "https://api.twitter.com/2/tweets";

        var body = new
        {
            text = message,
            reply = new { in_reply_to_tweet_id = replyToTweetId },
            media = new { media_ids = new[] { mediaId } },
        };

        string bodyJson = JsonSerializer.Serialize(body);
        var bodyBytes = Encoding.UTF8.GetBytes(bodyJson);

        var authHeader = GenerateOAuthHeader("POST", url);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "OAuth",
            authHeader.Substring(6)
        );
        request.Content = new ByteArrayContent(bodyBytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await _httpClient.SendAsync(request);
        string result = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"Resposta do Twitter ao postar reply com imagem: {result}");
    }

    /// <summary>
    /// Faz upload de uma imagem usando a biblioteca Tweetinvi
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    private async Task<string> UploadImageTweetinviAsync(string image)
    {
        if (image == null || image == string.Empty)
            throw new ArgumentNullException(nameof(image));

        var client = new TwitterClient(
            _xSettings.ApiKey,
            _xSettings.ApiSecret,
            _xSettings.AccessToken,
            _xSettings.AccessSecret
        );

        try
        {
            // Upload da imagem
            byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(image);
            var uploadedImage = await client.Upload.UploadBinaryAsync(imageBytes);

            if (uploadedImage == null)
            {
                Console.WriteLine("Erro ao fazer upload da imagem.");
                return null;
            }

            return uploadedImage.UploadedMediaInfo.MediaIdStr;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro: " + ex.Message);
        }

        return null;
    }

    /// <summary>
    /// Gera o cabeçalho OAuth para autenticação
    /// </summary>
    /// <param name="httpMethod"></param>
    /// <param name="url"></param>
    /// <param name="additionalParams"></param>
    /// <param name="isUpload"></param>
    /// <returns></returns>
    private string GenerateOAuthHeader(
        string httpMethod,
        string url,
        Dictionary<string, string> additionalParams = null,
        bool isUpload = false
    )
    {
        string nonce = Convert.ToBase64String(
            new ASCIIEncoding().GetBytes(DateTime.Now.Ticks.ToString())
        );
        string timestamp = Convert
            .ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds)
            .ToString();

        // Parâmetros OAuth
        var oauthParams = new SortedDictionary<string, string>
        {
            { "oauth_consumer_key", _xSettings.ApiKey },
            { "oauth_nonce", nonce },
            { "oauth_signature_method", "HMAC-SHA1" },
            { "oauth_timestamp", timestamp },
            { "oauth_token", _xSettings.AccessToken },
            { "oauth_version", "1.0" },
        };

        // NÃO incluir media_data aqui!
        var signatureParams = new SortedDictionary<string, string>(oauthParams);

        if (additionalParams != null && !isUpload)
        {
            foreach (var pair in additionalParams)
                signatureParams[pair.Key] = pair.Value;
        }

        string baseStringParams = string.Join(
            "&",
            signatureParams
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}")
        );

        string signatureBase =
            $"{httpMethod.ToUpper()}&{Uri.EscapeDataString(url)}&{Uri.EscapeDataString(baseStringParams)}";
        string signingKey =
            $"{Uri.EscapeDataString(_xSettings.ApiKey)}&{Uri.EscapeDataString(_xSettings.AccessSecret)}";

        using var hasher = new HMACSHA1(Encoding.ASCII.GetBytes(signingKey));
        string signature = Convert.ToBase64String(
            hasher.ComputeHash(Encoding.ASCII.GetBytes(signatureBase))
        );

        oauthParams["oauth_signature"] = signature;

        string header =
            "OAuth "
            + string.Join(
                ", ",
                oauthParams.Select(p => $"{p.Key}=\"{Uri.EscapeDataString(p.Value)}\"")
            );

        _logger.LogInformation($"Generated OAuth Header: {header}");
        return header;
    }

    /// <summary>
    /// Obtém as menções a um usuário
    /// </summary>
    /// <param name="startTime"></param>
    /// <returns></returns>
    public async Task<UserMentions?> GetUserMentions(string startTime = "")
    {
        if (string.IsNullOrEmpty(startTime))
            startTime = DateTime.UtcNow.AddMinutes(-15).ToString("yyyy-MM-ddTHH:mm:ssZ");

        string mentionsUrl =
            $"https://api.twitter.com/2/users/{_xSettings.UserId}/mentions?tweet.fields=author_id,created_at,text&start_time={startTime}";

        var response = await _httpClient.GetAsync(mentionsUrl);
        string responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Erro ao acessar a Twitter API: {response.StatusCode}");
            return null;
        }

        var mentions = JsonSerializer.Deserialize<UserMentions>(responseContent);
        return mentions;
    }

    /// <summary>
    /// Obtém a thread de um tweet
    /// </summary>
    /// <param name="tweetId"></param>
    /// <param name="authorId"></param>
    /// <returns></returns>
    public async Task<ThreadContext?> GetThreadContext(string tweetId, string authorId)
    {
        TweetContext tw = await GetTweetContext(tweetId);
        if (tw == null)
        {
            _logger.LogError("Erro ao buscar o tweet.");
            return null;
        }

        if (tw.RepliedToId == "")
        {
            // Se não é resposta a ninguém, então é o primeiro tweet da thread

            return new ThreadContext
            {
                Id = tweetId,
                AuthorA = tw.AuthorId,
                AuthorB = authorId,
                Tweets = new List<TweetContext> { tw },
            };
        }

        // Pega a thread que vem antes até aqui

        ThreadContext tc = await GetThreadContext(tw.RepliedToId, authorId);
        if (tc == null)
        {
            _logger.LogError("Erro ao buscar a thread.");
            return null;
        }

        if (tw.AuthorId == tc.AuthorA || tw.AuthorId == tc.AuthorB)
        {
            // Se o autor do tweet atual é o mesmo que o autor da thread ou o author da chamada, então adiciona o tweet atual à thread (outros autores são ignorados na thread)
            tc.Tweets.Add(tw);
        }

        return tc; // Retorna a thread atualizada
    }

    /// <summary>
    /// Finds all user references in the specified text that are prefixed with the '@' symbol.
    /// </summary>
    /// <remarks>User references are identified as sequences that start with '@' followed by one or
    /// more letters, digits, or underscores. The search is case-sensitive and does not validate whether the
    /// referenced users exist.</remarks>
    /// <param name="text">The text to search for user references. May be null or empty.</param>
    /// <returns>An array of strings containing all user references found in the text, each starting with '@'. Returns an
    /// empty array if no user references are found.</returns>
    public static string[] FindUsersReference(string text)
    {
        // Regex para encontrar @ seguido de letras, números e underscores
        string standard = @"@\w+";

        MatchCollection matches = Regex.Matches(text, standard);
        List<string> users = new List<string>();

        foreach (Match match in matches)
        {
            users.Add(match.Value);
        }

        return users.ToArray();
    }

    /// <summary>
    /// Remove todas as referências de usuários (ex: @usuario) do texto fornecido
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    private static string RemoveReferences(string text)
    {
        // Regex para encontrar @ seguido de letras, números e underscores
        string standard = @"@\w+";

        // Substitui todas as ocorrências por string vazia
        string result = Regex.Replace(text, standard, "").Trim();

        // Opcional: remover múltiplos espaços que sobraram
        result = Regex.Replace(result, @"\s{2,}", " ");

        return result.Trim();
    }
}
