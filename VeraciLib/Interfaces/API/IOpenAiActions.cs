using System;
using VeraciLib.Models.OpenAiApi;
using VeraciLib.Models.TwitterApi;

namespace VeraciLib.Interfaces.API;

public interface IOpenAiActions
{
    Task<IdentifiedCommand> CheckCommand(string commandStr, bool isSingle);
    Task<FullEvaluation> CheckThread(ThreadContext thread);
    Task<string> TranslatePhrase(string phrase, string lang);
}
