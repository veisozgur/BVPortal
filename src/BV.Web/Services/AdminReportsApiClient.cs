using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class AdminReportsApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    public async Task<AdminReportSummaryModel?> GetSummaryAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin || string.IsNullOrWhiteSpace(session.AccessToken))
            return null;

        var client = httpClientFactory.CreateClient("BV.Api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return await client.GetFromJsonAsync<AdminReportSummaryModel>(
            $"api/v1/admin/reports/summary?days={Math.Clamp(days, 1, 365)}",
            cancellationToken);
    }
}

public sealed record AdminReportSummaryModel(
    int PeriodDays,
    int Total,
    int Submitted,
    int Answered,
    int Accepted,
    int Rejected,
    int School,
    int Office,
    decimal AnswerRate,
    decimal ConversionRate,
    decimal AverageResponseMinutes,
    IReadOnlyList<AdminDailyReportModel> Daily,
    IReadOnlyList<AdminTopProductModel> TopProducts);

public sealed record AdminDailyReportModel(
    DateTime Date,
    int Created,
    int Answered,
    int Accepted);

public sealed record AdminTopProductModel(
    string ProductName,
    int RequestCount,
    decimal TotalQuantity);
