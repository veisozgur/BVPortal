using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class AdminOperationTaskApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<IReadOnlyList<AdminOperationTaskModel>> ListAsync(Guid? orderId = null, int? status = null, CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin) return [];
        var path = "api/v1/admin/operation-tasks";
        var query = new List<string>();
        if (orderId.HasValue) query.Add($"orderId={orderId.Value}");
        if (status.HasValue) query.Add($"status={status.Value}");
        if (query.Count > 0) path += "?" + string.Join("&", query);
        return await CreateClient().GetFromJsonAsync<List<AdminOperationTaskModel>>(path, cancellationToken) ?? [];
    }

    public async Task<ApiResult> CreateAsync(Guid orderId, string title, string? description, int priority, DateTime? dueAtUtc, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PostAsJsonAsync("api/v1/admin/operation-tasks", new { orderId, title, description, priority, dueAtUtc, assignedUserId = (Guid?)null }, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Ok("Görev oluşturuldu.") : ApiResult.Fail($"Görev oluşturulamadı ({(int)response.StatusCode}).");
    }

    public async Task<ApiResult> ChangeStatusAsync(Guid id, int status, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PutAsJsonAsync($"api/v1/admin/operation-tasks/{id}/status", new { status }, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Ok("Görev durumu güncellendi.") : ApiResult.Fail($"Görev durumu güncellenemedi ({(int)response.StatusCode}).");
    }
}

public sealed record AdminOperationTaskModel(Guid Id, Guid OrderId, Guid? AssignedUserId, string Title, string? Description, int Priority, int Status, DateTime? DueAtUtc, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, DateTime? CompletedAtUtc);
