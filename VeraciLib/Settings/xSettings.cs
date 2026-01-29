using System;

namespace VeraciLib.Settings;

public class xSettings : SettingsBase
{
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string AccessToken { get; set; }
    public string AccessSecret { get; set; }
    public string BearerToken { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string ApiSecret { get; set; }
}
