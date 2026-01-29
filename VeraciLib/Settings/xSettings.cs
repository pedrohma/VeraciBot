using System;

namespace VeraciLib.Settings;

public class xSettings : SettingsBase
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string AccessSecret { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}
