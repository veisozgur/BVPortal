using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class AdminSchoolApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<IReadOnlyList<SchoolListItemModel>> ListAsync(string? search = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin)
            return [];

        var path = "api/v1/admin/schools";
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (isActive.HasValue) query.Add($"isActive={isActive.Value.ToString().ToLowerInvariant()}");
        if (query.Count > 0) path += "?" + string.Join("&", query);

        return await CreateClient().GetFromJsonAsync<List<SchoolListItemModel>>(path, cancellationToken) ?? [];
    }

    public async Task<SchoolDetailModel?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        !session.IsAdmin ? null : await CreateClient().GetFromJsonAsync<SchoolDetailModel>($"api/v1/admin/schools/{id}", cancellationToken);

    public async Task<ApiResult> SaveAsync(Guid? id, SaveSchoolModel model, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var response = id.HasValue
            ? await client.PutAsJsonAsync($"api/v1/admin/schools/{id.Value}", model, cancellationToken)
            : await client.PostAsJsonAsync("api/v1/admin/schools", model, cancellationToken);

        return await ToResultAsync(response, id.HasValue ? "Okul güncellendi." : "Okul oluşturuldu.", cancellationToken);
    }

    public async Task<ApiResult> SaveGradeAsync(Guid schoolId, Guid? gradeId, SaveSchoolGradeModel model, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var response = gradeId.HasValue
            ? await client.PutAsJsonAsync($"api/v1/admin/schools/{schoolId}/grades/{gradeId.Value}", model, cancellationToken)
            : await client.PostAsJsonAsync($"api/v1/admin/schools/{schoolId}/grades", model, cancellationToken);

        return await ToResultAsync(response, gradeId.HasValue ? "Sınıf/kademe güncellendi." : "Sınıf/kademe eklendi.", cancellationToken);
    }

    private static async Task<ApiResult> ToResultAsync(HttpResponseMessage response, string successMessage, CancellationToken cancellationToken)
    {
        ApiMessage? body = null;
        try { body = await response.Content.ReadFromJsonAsync<ApiMessage>(cancellationToken: cancellationToken); } catch { }
        return response.IsSuccessStatusCode
            ? ApiResult.Ok(body?.Message ?? successMessage)
            : ApiResult.Fail(body?.Message ?? $"İşlem başarısız ({(int)response.StatusCode}).");
    }
}

public sealed record SchoolListItemModel(Guid Id, string Name, string? Code, string? ContactName, string? Phone, string? Email, string? Address, bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, int GradeCount);
public sealed record SchoolDetailModel(Guid Id, string Name, string? Code, string? ContactName, string? Phone, string? Email, string? Address, bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, IReadOnlyList<SchoolGradeModel> Grades);
public sealed record SchoolGradeModel(Guid Id, string Name, int SortOrder, bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
public sealed record SaveSchoolModel(string Name, string? Code, string? ContactName, string? Phone, string? Email, string? Address, bool IsActive);
public sealed record SaveSchoolGradeModel(string Name, int SortOrder, bool IsActive);
