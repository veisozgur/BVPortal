using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class AdminNotificationApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<NotificationPageModel?> ListAsync(
        string? channel = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin)
            return null;

        var parameters = new List<string> { "page=1", "pageSize=100" };
        if (!string.IsNullOrWhiteSpace(channel))
            parameters.Add($"channel={Uri.EscapeDataString(channel)}");
        if (!string.IsNullOrWhiteSpace(status))
            parameters.Add($"status={Uri.EscapeDataString(status)}");

        return await CreateClient().GetFromJsonAsync<NotificationPageModel>(
            $"api/v1/admin/notifications?{string.Join('&', parameters)}",
            cancellationToken);
    }

    public async Task<ApiResult> RetryAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PostAsync(
            $"api/v1/admin/notifications/{notificationId}/retry",
            content: null,
            cancellationToken);

        return response.IsSuccessStatusCode
            ? ApiResult.Ok("Bildirim yeniden gönderildi.")
            : ApiResult.Fail($"Bildirim yeniden gönderilemedi ({(int)response.StatusCode}).");
    }
}

public sealed record NotificationPageModel(
    IReadOnlyList<NotificationListItemModel> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int SentCount,
    int FailedCount,
    int PendingCount);

public sealed record NotificationListItemModel(
    Guid Id,
    Guid QuoteRequestId,
    string Title,
    string Channel,
    string Destination,
    string Status,
    string? ErrorMessage,
    DateTime CreatedAtUtc,
    DateTime? SentAtUtc);
