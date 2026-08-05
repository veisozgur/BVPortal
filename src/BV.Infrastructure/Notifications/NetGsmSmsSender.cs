using System.Net.Http.Json;
using BV.Application.Abstractions.Notifications;
using Microsoft.Extensions.Options;

namespace BV.Infrastructure.Notifications;

public sealed class NetGsmSmsSender(HttpClient httpClient, IOptions<NetGsmOptions> options) : ISmsSender
{
    private readonly NetGsmOptions _options = options.Value;

    public async Task SendAsync(string phone, string message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            throw new InvalidOperationException("NetGSM production sender is disabled.");

        var payload = new
        {
            usercode = _options.UserCode,
            password = _options.Password,
            msgheader = _options.Header,
            messages = new[] { new { msg = message, no = NormalizePhone(phone) } },
            encoding = _options.Language
        };

        using var response = await httpClient.PostAsJsonAsync("sms/rest/v2/send", payload, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"NetGSM SMS gönderimi başarısız. HTTP {(int)response.StatusCode}: {responseBody}");
    }

    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        return digits.StartsWith("90", StringComparison.Ordinal) ? digits : $"90{digits.TrimStart('0')}";
    }
}
