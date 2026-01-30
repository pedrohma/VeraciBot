using System;

namespace VeraciLib.Models.OpenAiApi;

public class FullEvaluation
{
    public int Result { get; set; } = 0; // 1 a 5
    public string Response { get; set; } = string.Empty;
    public string Language { get; set; } = "";
}
