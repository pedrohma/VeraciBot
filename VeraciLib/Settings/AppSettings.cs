using System;

namespace VeraciLib.Settings;

public class AppSettings
{
    public xSettings xSettings { get; set; }
    public SettingsBase SendGridSettings { get; set; }
    public SettingsBase OpenAISettings { get; set; }
    public DatabaseSettings DatabaseSettings { get; set; }
}
