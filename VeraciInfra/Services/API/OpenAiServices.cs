using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VeraciLib.Interfaces.API;
using VeraciLib.Models;
using VeraciLib.Models.OpenAiApi;
using VeraciLib.Models.TwitterApi;
using VeraciLib.Settings;

namespace VeraciInfra.Services.API;

public class OpenAiServices : IOpenAiActions
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiServices> _logger;
    private readonly string _openAiApiKey;
    public static string cmdPromptCmdSingle =
        "Identifique no prompt o que o usuário quer de resposta do sistema entre "
        + "1='Ajuda, como funciona o sistema', "
        + "2='Ver minha pontuação', "
        + "3='Ver o quadro de líderes', "
        + "10='Quero convidar outra pessoa', "
        + "20='Aceito o convite' "
        + "21='Não aceito o convite' ";

    public static string cmdPromptCmdThread =
        "30='Acho que esse conteudo é falso', "
        + "31='Estou argumentando algo diferente ao conteúdo anterior', "
        + "32='Quem está certo nessa discussão' ";

    public const int CMD_UNKNOWN = 0;
    public const int CMD_HELP = 1;
    public const int CMD_SCORE = 2;
    public const int CMD_SCOREBOARD = 3;
    public const int CMD_INVITE = 10;
    public const int CMD_ACCEPT_INVITE = 20;
    public const int CMD_REFUSE_INVITE = 21;
    public const int CMD_THREAD_FALSE = 30;
    public const int CMD_THREAD_ARGUE = 31;
    public const int CMD_THREAD_WHOISRIGHT = 32;

    public static string cmdPromptCmdEnd =
        "ou então 0 se o comando não identificado ou qualquer outro comando.";

    public static string cmdPromptLanguage =
        "Você deve identificar qual a lingua usada pelo usuário. ";

    public static string cmdPromptCmdFinal =
        "Responda com o número do comando, seguido da lingua, separados por vírgula.";

    public OpenAiServices(AppSettings appSettings, ILogger<OpenAiServices> logger)
    {
        if (appSettings == null)
        {
            throw new ArgumentNullException(nameof(appSettings));
        }
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }
        _logger = logger;
        _openAiApiKey = appSettings.OpenAISettings.ApiKey;
        if (string.IsNullOrEmpty(_openAiApiKey))
        {
            throw new Exception("Null OpenAI Api Key");
        }
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _openAiApiKey
        );
    }

    public async Task<IdentifiedCommand> CheckCommand(string commandStr, bool isSingle)
    {
        if (commandStr == null)
        {
            throw new ArgumentNullException(nameof(commandStr));
        }

        IdentifiedCommand resp = new IdentifiedCommand() { Language = "pt", Result = 0 };

        // Se comando é vazio, só tem duas possibilidades, não precisa perguntar pro chatgpt

        if (commandStr == null || commandStr == "" || commandStr.Length < 5)
        {
            resp.Result = CMD_THREAD_FALSE;
            if (isSingle)
                resp.Result = CMD_HELP;
            resp.Language = ""; // Não tem como saber a lingua (pegar pelo usuário)
            return resp;
        }

        var requestBody = new
        {
            model = "gpt-4o",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = cmdPromptLanguage
                        + cmdPromptCmdSingle
                        + (isSingle ? "" : cmdPromptCmdThread)
                        + cmdPromptCmdEnd
                        + cmdPromptCmdFinal,
                },
                new { role = "user", content = commandStr },
            },
            temperature = 0.2,
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );
        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content
        );
        var responseString = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"OpenAI Response: {responseString}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"OpenAI API request failed with status code {response.StatusCode}: {responseString}"
            );
        }

        using var doc = JsonDocument.Parse(responseString);
        string baseResult = doc
            .RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            .Trim();

        if (baseResult == null || baseResult.Length < 3 || !baseResult.Contains(","))
            return resp; // Resposta inválida

        // Pega comando e lingua

        string[] numberPart = baseResult.Split(",");

        if (numberPart.Length > 0)
            resp.Result = int.TryParse(numberPart[0].Trim(), out int cmd) ? cmd : 0;

        if (numberPart.Length > 1)
            resp.Language = numberPart[1].Trim();

        return resp;
    }

    public async Task<FullEvaluation> CheckThread(ThreadContext thread)
    {
        var requestBody = new
        {
            model = "gpt-4o",
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "Você é um juiz muito duro e rigoroso. Você recebe uma conversa entre duas pessoas em que a primeira pessoa afirma algo e a segunda quer contra-argumentar. Você deve resonder com um número entre 1 e 5 dizendo o quanto a primeira pessoa da conversa está correta. 1 significa que a primeira pessoa tem plena razão e a segunda está errada, até 5 em que a primeira pessoa está totalmente errada e a segunda certa. Inclua após o número sua resposta com tons de ironia e sarcasmo dizendo o resultado da sua avaliação.",
                },
                new { role = "user", content = thread.GetFullDialog() },
            },
            temperature = 0.2,
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );
        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content
        );
        var responseString = await response.Content.ReadAsStringAsync();
        _logger.LogInformation($"OpenAI Response: {responseString}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"OpenAI API request failed with status code {response.StatusCode}: {responseString}"
            );
        }

        using var doc = JsonDocument.Parse(responseString);
        string baseResult = doc
            .RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()
            .Trim();

        FullEvaluation resp = new FullEvaluation();

        string numberPart = baseResult.Substring(0, 1); // Pega apenas o primeiro caractere da resposta

        // Tenta converter a resposta para número
        resp.Result = int.TryParse(numberPart, out int nota) ? nota : -1;
        resp.Response = baseResult.Substring(2).Trim(); // Pega o restante da resposta após o número

        if (resp.Response.StartsWith("-"))
            resp.Response = resp.Response.Substring(2).Trim();

        return resp;
    }

    public async Task<string> TranslatePhrase(string phrase, string lang)
    {
        return phrase; // Desativado temporariamente

        // using var client = new HttpClient();
        // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
        //     "Bearer",
        //     AppKeys.keys.openAIKey
        // );

        // var requestBody = new
        // {
        //     model = "gpt-4o",
        //     messages = new[]
        //     {
        //         new
        //         {
        //             role = "system",
        //             content = "Retorne a mesma frase, com o mesmo sentido, traduzindo para a lingua \""
        //                 + lang
        //                 + "\"",
        //         },
        //         new { role = "user", content = phrase },
        //     },
        //     temperature = 4.0,
        // };

        // var content = new StringContent(
        //     JsonSerializer.Serialize(requestBody),
        //     Encoding.UTF8,
        //     "application/json"
        // );
        // var response = await client.PostAsync(
        //     "https://api.openai.com/v1/chat/completions",
        //     content
        // );
        // var responseString = await response.Content.ReadAsStringAsync();

        // if (!response.IsSuccessStatusCode)
        // {
        //     Console.WriteLine("Erro ao acessar a OpenAI API:");
        //     Console.WriteLine(responseString);
        //     return null;
        // }

        // using var doc = JsonDocument.Parse(responseString);
        // string resp = doc
        //     .RootElement.GetProperty("choices")[0]
        //     .GetProperty("message")
        //     .GetProperty("content")
        //     .GetString()
        //     .Trim();

        // if (resp == null || resp == string.Empty || resp.Length < 5)
        // {
        //     return phrase;
        // }

        // return resp;
    }
}
