namespace BV.Infrastructure.Notifications;

public sealed class NetGsmOptions
{
    public const string SectionName = "NetGsm";

    public bool Enabled { get; init; }
    public string UserCode { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Header { get; init; } = string.Empty;
    public string Language { get; init; } = "TR";
    public string BaseUrl { get; init; } = "https://api.netgsm.com.tr";
}
