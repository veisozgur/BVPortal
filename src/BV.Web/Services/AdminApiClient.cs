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

    public async Task<AdminQuoteDetailModel?> GetQuoteDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
            return null;

        return await CreateClient().GetFromJsonAsync<AdminQuoteDetailModel>(
            $"api/v1/admin/quote-requests/{id}", cancellationToken);
    }

    public async Task<ApiResult> ChangeQuoteStatusAsync(Guid id, int status, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PutAsJsonAsync(
            $"api/v1/admin/quote-requests/{id}/status", new { status }, cancellationToken);

        return response.IsSuccessStatusCode
            ? ApiResult.Ok("Teklif durumu güncellendi.")
            : ApiResult.Fail($"Teklif durumu güncellenemedi ({(int)response.StatusCode}).");
    }

    public async Task<IReadOnlyList<AdminOperationNoteModel>> ListOperationNotesAsync(
        Guid quoteRequestId,
        CancellationToken cancellationToken = default)
    {
        return await CreateClient().GetFromJsonAsync<List<AdminOperationNoteModel>>(
            $"api/v1/admin/quote-requests/{quoteRequestId}/notes", cancellationToken) ?? [];
    }

    public async Task<ApiResult> AddOperationNoteAsync(
        Guid quoteRequestId,
        string note,
        CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PostAsJsonAsync(
            $"api/v1/admin/quote-requests/{quoteRequestId}/notes", new { note }, cancellationToken);

        return response.IsSuccessStatusCode
            ? ApiResult.Ok("Operasyon notu eklendi.")
            : ApiResult.Fail($"Operasyon notu eklenemedi ({(int)response.StatusCode}).");
    }

    public async Task<ApiResult> AnswerQuoteAsync(
        Guid quoteRequestId,
        AdminAnswerQuoteModel model,
        CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PostAsJsonAsync(
            $"api/v1/admin/quote-requests/{quoteRequestId}/response", model, cancellationToken);

        if (response.IsSuccessStatusCode)
            return ApiResult.Ok("Fiyat teklifi kaydedildi ve müşteriye gönderildi.");

        ApiMessage? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiMessage>(cancellationToken: cancellationToken);
        }
        catch
        {
            // Generic error below.
        }

        return ApiResult.Fail(error?.Message ?? $"Fiyat teklifi gönderilemedi ({(int)response.StatusCode}).");
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

public sealed record AdminQuoteDetailModel(
    Guid Id,
    int Type,
    int Status,
    string Title,
    string? Description,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    string? OrganizationName,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? AnsweredAtUtc,
    IReadOnlyList<AdminQuoteDetailItemModel> Items,
    AdminQuoteResponseDetailModel? Response);

public sealed record AdminQuoteDetailItemModel(
    Guid Id,
    string ProductName,
    decimal Quantity,
    string Unit,
    string? Notes);

public sealed record AdminQuoteResponseDetailModel(
    Guid Id,
    string Message,
    DateTime ValidUntilUtc,
    DateTime? SentAtUtc,
    decimal TotalAmount,
    IReadOnlyList<AdminQuoteResponseItemModel> Items);

public sealed record AdminQuoteResponseItemModel(
    Guid Id,
    string ProductName,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal);

public sealed record AdminOperationNoteModel(
    Guid Id,
    Guid QuoteRequestId,
    Guid CreatedByUserId,
    string Note,
    DateTime CreatedAtUtc);

public sealed class AdminAnswerQuoteModel
{
    public string Message { get; set; } = "Talebiniz doğrultusunda fiyat teklifimiz hazırlanmıştır.";
    public DateTime ValidUntilUtc { get; set; } = DateTime.UtcNow.AddDays(7);
    public bool NotifyBySms { get; set; } = true;
    public bool NotifyByEmail { get; set; } = true;
    public List<AdminAnswerQuoteItemModel> Items { get; } = [];
}

public sealed class AdminAnswerQuoteItemModel
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "Adet";
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; } = 20;
}
