using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class IntegrationStatusApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    public async Task<IntegrationStatusModel?> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin || string.IsNullOrWhiteSpace(session.AccessToken))
            return null;

        var client = httpClientFactory.CreateClient("BV.Api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return await client.GetFromJsonAsync<IntegrationStatusModel>("api/v1/admin/integrations/status", cancellationToken);
    }
}

public sealed record IntegrationStatusModel(
    IntegrationProviderStatus NetGsm,
    IntegrationProviderStatus Email,
    IntegrationProviderStatus Mikro);

public sealed record IntegrationProviderStatus(
    string Name,
    bool Enabled,
    bool Configured,
    bool Ready,
    string Message);
