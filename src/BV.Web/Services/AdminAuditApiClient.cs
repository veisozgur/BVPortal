using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class AdminAuditApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<AdminAuditPageModel?> ListAsync(
        string? method = null,
        int? statusCode = null,
        string? search = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin)
            return null;

        var path = $"api/v1/admin/audit-logs?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(method))
            path += $"&method={Uri.EscapeDataString(method)}";
        if (statusCode.HasValue)
            path += $"&statusCode={statusCode.Value}";
        if (!string.IsNullOrWhiteSpace(search))
            path += $"&search={Uri.EscapeDataString(search.Trim())}";

        return await CreateClient().GetFromJsonAsync<AdminAuditPageModel>(path, cancellationToken);
    }
}

public sealed record AdminAuditPageModel(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<AdminAuditLogModel> Items);

public sealed record AdminAuditLogModel(
    Guid Id,
    Guid? UserId,
    string Action,
    string Method,
    string Path,
    string? IpAddress,
    int StatusCode,
    DateTime CreatedAtUtc);
