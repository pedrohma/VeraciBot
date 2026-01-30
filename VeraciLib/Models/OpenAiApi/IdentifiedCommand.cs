using System;

namespace VeraciLib.Models.OpenAiApi;

public class IdentifiedCommand
{
 public int Result { get; set; } = 0; // 0 = Não Identificado, 1 = Auda, 2 = Pontuação, 3 = Scoreboard, etc vide abaixo
    public string Language { get; set; } = "pt";
}
