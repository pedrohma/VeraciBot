using System;
using VeraciBotCore.APIs.OpenAI;
using VeraciBotCore.APIs.Twitter;

namespace VeraciLib.Interfaces;

public interface IOpenAiActions
{
    Task<IdentifiedCommand> CheckCommand(string commandStr, bool isSingle);
    Task<FullEvaluation> CheckThread(ThreadContext thread);
    Task<string> TranslatePhrase(string phrase, string lang);
}
