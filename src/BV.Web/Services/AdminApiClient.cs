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

        return await CreateClient().GetFromJsonAsync<AdminDashboardSummaryModel>(
            "api/v1/admin/dashboard", cancellationToken);
    }

    public async Task<IReadOnlyList<AdminDailyMetricModel>> GetDailyMetricsAsync(
        int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
            return [];

        return await CreateClient().GetFromJsonAsync<List<AdminDailyMetricModel>>(
            $"api/v1/admin/dashboard/daily-metrics?days={days}", cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<AdminQuoteListItemModel>> ListQuotesAsync(
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
            return [];

        var path = "api/v1/admin/quote-requests?page=1&pageSize=50";
        if (!string.IsNullOrWhiteSpace(search))
            path += $"&search={Uri.EscapeDataString(search)}";

        return await CreateClient().GetFromJsonAsync<List<AdminQuoteListItemModel>>(path, cancellationToken) ?? [];
    }
}

public sealed record AdminDashboardSummaryModel(
    int TotalCustomers,
    int TotalQuoteRequests,
    int PendingQuoteRequests,
    int AnsweredQuoteRequests,
    int FailedNotifications);

public sealed record AdminDailyMetricModel(
    DateOnly Date,
    int CreatedQuoteRequests,
    int SubmittedQuoteRequests,
    int AnsweredQuoteRequests);

public sealed record AdminQuoteListItemModel(
    Guid Id,
    int Type,
    int Status,
    string Title,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    int ItemCount,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? AnsweredAtUtc);
