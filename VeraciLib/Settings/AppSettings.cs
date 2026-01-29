using System;

namespace VeraciLib.Settings;

public class AppSettings
{
    public xSettings xSettings { get; set; } = new xSettings();
    public SettingsBase SendGridSettings { get; set; }
    public SettingsBase OpenAISettings { get; set; }
    public DatabaseSettings DatabaseSettings { get; set; } = new DatabaseSettings();
}
