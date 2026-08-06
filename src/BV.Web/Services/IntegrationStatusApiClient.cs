using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class IntegrationStatusApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<IntegrationStatusModel?> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin || string.IsNullOrWhiteSpace(session.AccessToken))
            return null;

        return await CreateClient().GetFromJsonAsync<IntegrationStatusModel>("api/v1/admin/integrations/status", cancellationToken);
    }

    public async Task<ApiResult<MikroSyncResultModel>> SyncMikroCatalogAsync(CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin || string.IsNullOrWhiteSpace(session.AccessToken))
            return ApiResult<MikroSyncResultModel>.Fail("Yönetici oturumu gerekli.");

        using var response = await CreateClient().PostAsync("api/v1/admin/integrations/mikro/sync-catalog", null, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<MikroSyncResultModel>(cancellationToken: cancellationToken);
            return result is null
                ? ApiResult<MikroSyncResultModel>.Fail("Senkronizasyon sonucu okunamadı.")
                : ApiResult<MikroSyncResultModel>.Ok(result, "Mikro katalog senkronizasyonu tamamlandı.");
        }

        ApiMessage? error = null;
        try { error = await response.Content.ReadFromJsonAsync<ApiMessage>(cancellationToken: cancellationToken); } catch { }
        return ApiResult<MikroSyncResultModel>.Fail(error?.Message ?? $"Senkronizasyon başarısız ({(int)response.StatusCode}).");
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

public sealed record MikroSyncResultModel(
    int CreatedCategories,
    int CreatedProducts,
    int UpdatedProducts,
    int SkippedRows);
