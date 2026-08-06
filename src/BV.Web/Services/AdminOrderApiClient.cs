using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class AdminOrderApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<AdminOrderPageModel?> ListAsync(
        int? status = null,
        string? search = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin)
            return null;

        var path = $"api/v1/admin/orders?page={page}&pageSize={pageSize}";
        if (status.HasValue)
            path += $"&status={status.Value}";
        if (!string.IsNullOrWhiteSpace(search))
            path += $"&search={Uri.EscapeDataString(search.Trim())}";

        return await CreateClient().GetFromJsonAsync<AdminOrderPageModel>(path, cancellationToken);
    }

    public async Task<AdminOrderDetailModel?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        !session.IsAdmin
            ? null
            : await CreateClient().GetFromJsonAsync<AdminOrderDetailModel>($"api/v1/admin/orders/{id}", cancellationToken);

    public async Task<IReadOnlyList<OrderTimelineItemModel>> GetTimelineAsync(Guid id, CancellationToken cancellationToken = default) =>
        !session.IsAdmin
            ? []
            : await CreateClient().GetFromJsonAsync<List<OrderTimelineItemModel>>($"api/v1/orders/{id}/timeline", cancellationToken) ?? [];

    public async Task<OrderSyncStatusModel?> GetSyncStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin)
            return null;

        var response = await CreateClient().GetAsync($"api/v1/admin/orders/{id}/sync", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<OrderSyncStatusModel>(cancellationToken: cancellationToken);
    }

    public async Task<ApiResult> SyncToMikroAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PostAsync($"api/v1/admin/orders/{id}/sync", null, cancellationToken);
        ApiMessage? body = null;
        try { body = await response.Content.ReadFromJsonAsync<ApiMessage>(cancellationToken: cancellationToken); } catch { }

        return response.IsSuccessStatusCode
            ? ApiResult.Ok(body?.Message ?? "Sipariş Mikro'ya aktarıldı.")
            : ApiResult.Fail(body?.Message ?? $"Mikro aktarımı başarısız ({(int)response.StatusCode}).");
    }

    public async Task<ApiResult> ChangeStatusAsync(
        Guid id,
        int status,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PutAsJsonAsync(
            $"api/v1/admin/orders/{id}/status",
            new { status, note },
            cancellationToken);

        ApiMessage? body = null;
        try { body = await response.Content.ReadFromJsonAsync<ApiMessage>(cancellationToken: cancellationToken); } catch { }

        return response.IsSuccessStatusCode
            ? ApiResult.Ok("Sipariş durumu güncellendi.")
            : ApiResult.Fail(body?.Message ?? $"Sipariş durumu güncellenemedi ({(int)response.StatusCode}).");
    }

    public async Task<ApiResult> UpdateNotesAsync(
        Guid id,
        string? customerNote,
        string? internalNote,
        CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PutAsJsonAsync(
            $"api/v1/admin/orders/{id}/notes",
            new { customerNote, internalNote },
            cancellationToken);

        ApiMessage? body = null;
        try { body = await response.Content.ReadFromJsonAsync<ApiMessage>(cancellationToken: cancellationToken); } catch { }

        return response.IsSuccessStatusCode
            ? ApiResult.Ok(body?.Message ?? "Sipariş notları güncellendi.")
            : ApiResult.Fail(body?.Message ?? $"Sipariş notları güncellenemedi ({(int)response.StatusCode}).");
    }
}

public sealed record AdminOrderPageModel(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<AdminOrderListItemModel> Items);

public sealed record AdminOrderListItemModel(
    Guid Id,
    string OrderNumber,
    Guid QuoteRequestId,
    Guid CustomerId,
    int Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? CompletedAtUtc,
    decimal TotalAmount,
    int ItemCount);

public sealed record AdminOrderDetailModel(
    Guid Id,
    string OrderNumber,
    Guid QuoteRequestId,
    Guid QuoteResponseId,
    Guid CustomerId,
    int Status,
    string? CustomerNote,
    string? InternalNote,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? CompletedAtUtc,
    decimal TotalAmount,
    IReadOnlyList<AdminOrderItemModel> Items);

public sealed record AdminOrderItemModel(
    Guid Id,
    string ProductName,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal);

public sealed record OrderSyncStatusModel(
    Guid Id,
    Guid OrderId,
    string Provider,
    int Status,
    string? ExternalOrderId,
    int AttemptCount,
    DateTime? LastAttemptAtUtc,
    DateTime? LastSuccessAtUtc,
    string? ErrorMessage);
