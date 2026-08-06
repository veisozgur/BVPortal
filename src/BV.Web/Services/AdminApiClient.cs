using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class AdminApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<AdminDashboardSummaryModel?> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
            return null;
        return await CreateClient().GetFromJsonAsync<AdminDashboardSummaryModel>("api/v1/admin/dashboard", cancellationToken);
    }

    public async Task<IReadOnlyList<AdminDailyMetricModel>> GetDailyMetricsAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
            return [];
        return await CreateClient().GetFromJsonAsync<List<AdminDailyMetricModel>>($"api/v1/admin/dashboard/daily-metrics?days={days}", cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<AdminQuoteListItemModel>> ListQuotesAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
            return [];
        var path = "api/v1/admin/quote-requests?page=1&pageSize=50";
        if (!string.IsNullOrWhiteSpace(search))
            path += $"&search={Uri.EscapeDataString(search)}";
        return await CreateClient().GetFromJsonAsync<List<AdminQuoteListItemModel>>(path, cancellationToken) ?? [];
    }

    public async Task<AdminQuoteDetailModel?> GetQuoteDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin)
            return null;
        return await CreateClient().GetFromJsonAsync<AdminQuoteDetailModel>($"api/v1/admin/quote-requests/{id}", cancellationToken);
    }

    public async Task<ApiResult> ChangeQuoteStatusAsync(Guid id, int status, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PutAsJsonAsync($"api/v1/admin/quote-requests/{id}/status", new { status }, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Ok("Teklif durumu güncellendi.") : ApiResult.Fail($"Durum güncellenemedi ({(int)response.StatusCode}).");
    }
}

public sealed record AdminDashboardSummaryModel(int TotalCustomers, int TotalQuoteRequests, int PendingQuoteRequests, int AnsweredQuoteRequests, int FailedNotifications);
public sealed record AdminDailyMetricModel(DateOnly Date, int CreatedQuoteRequests, int SubmittedQuoteRequests, int AnsweredQuoteRequests);
public sealed record AdminQuoteListItemModel(Guid Id, int Type, int Status, string Title, string CustomerName, string CustomerPhone, string CustomerEmail, int ItemCount, DateTime CreatedAtUtc, DateTime? SubmittedAtUtc, DateTime? AnsweredAtUtc);
public sealed record AdminQuoteDetailModel(Guid Id, int Type, int Status, string Title, string? Description, string CustomerName, string CustomerPhone, string CustomerEmail, string? OrganizationName, DateTime CreatedAtUtc, DateTime? SubmittedAtUtc, DateTime? AnsweredAtUtc, IReadOnlyList<AdminQuoteDetailItemModel> Items, AdminQuoteResponseDetailModel? Response);
public sealed record AdminQuoteDetailItemModel(Guid Id, string ProductName, decimal Quantity, string Unit, string? Notes);
public sealed record AdminQuoteResponseDetailModel(Guid Id, string Message, DateTime ValidUntilUtc, DateTime? SentAtUtc, decimal TotalAmount, IReadOnlyList<AdminQuoteResponseItemModel> Items);
public sealed record AdminQuoteResponseItemModel(Guid Id, string ProductName, decimal Quantity, string Unit, decimal UnitPrice, decimal VatRate, decimal LineTotal);
