using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class AdminSchoolSupplySetApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<IReadOnlyList<SchoolSupplySetListItemModel>> ListAsync(Guid? schoolId = null, Guid? gradeId = null, int? academicYear = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        if (!session.IsAdmin) return [];
        var query = new List<string>();
        if (schoolId.HasValue) query.Add($"schoolId={schoolId.Value}");
        if (gradeId.HasValue) query.Add($"gradeId={gradeId.Value}");
        if (academicYear.HasValue) query.Add($"academicYear={academicYear.Value}");
        if (isActive.HasValue) query.Add($"isActive={isActive.Value.ToString().ToLowerInvariant()}");
        var path = "api/v1/admin/supply-sets" + (query.Count == 0 ? string.Empty : "?" + string.Join("&", query));
        return await CreateClient().GetFromJsonAsync<List<SchoolSupplySetListItemModel>>(path, cancellationToken) ?? [];
    }

    public async Task<SchoolSupplySetDetailModel?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        !session.IsAdmin ? null : await CreateClient().GetFromJsonAsync<SchoolSupplySetDetailModel>($"api/v1/admin/supply-sets/{id}", cancellationToken);

    public async Task<ApiResult> CreateAsync(SaveSchoolSupplySetModel model, CancellationToken cancellationToken = default) =>
        await ToResultAsync(await CreateClient().PostAsJsonAsync("api/v1/admin/supply-sets", model, cancellationToken), "Okul seti oluşturuldu.", cancellationToken);

    public async Task<ApiResult> UpdateAsync(Guid id, UpdateSchoolSupplySetModel model, CancellationToken cancellationToken = default) =>
        await ToResultAsync(await CreateClient().PutAsJsonAsync($"api/v1/admin/supply-sets/{id}", model, cancellationToken), "Okul seti güncellendi.", cancellationToken);

    public async Task<ApiResult> AddItemAsync(Guid id, SaveSchoolSupplySetItemModel model, CancellationToken cancellationToken = default) =>
        await ToResultAsync(await CreateClient().PostAsJsonAsync($"api/v1/admin/supply-sets/{id}/items", model, cancellationToken), "Set kalemi eklendi.", cancellationToken);

    public async Task<ApiResult> DeleteItemAsync(Guid id, Guid itemId, CancellationToken cancellationToken = default) =>
        await ToResultAsync(await CreateClient().DeleteAsync($"api/v1/admin/supply-sets/{id}/items/{itemId}", cancellationToken), "Set kalemi silindi.", cancellationToken);

    public async Task<ApiResult> CopyAsync(Guid id, int academicYear, string? name, CancellationToken cancellationToken = default) =>
        await ToResultAsync(await CreateClient().PostAsJsonAsync($"api/v1/admin/supply-sets/{id}/copy", new { academicYear, name }, cancellationToken), "Set yeni eğitim yılına kopyalandı.", cancellationToken);

    private static async Task<ApiResult> ToResultAsync(HttpResponseMessage response, string successMessage, CancellationToken cancellationToken)
    {
        ApiMessage? body = null;
        try { body = await response.Content.ReadFromJsonAsync<ApiMessage>(cancellationToken: cancellationToken); } catch { }
        return response.IsSuccessStatusCode
            ? ApiResult.Ok(body?.Message ?? successMessage)
            : ApiResult.Fail(body?.Message ?? $"İşlem başarısız ({(int)response.StatusCode}).");
    }
}

public sealed record SchoolSupplySetListItemModel(Guid Id, Guid SchoolId, string? SchoolName, Guid SchoolGradeId, string? GradeName, string Name, int AcademicYear, string? Description, bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, int ItemCount);
public sealed record SchoolSupplySetDetailModel(Guid Id, SchoolReferenceModel School, SchoolReferenceModel Grade, string Name, int AcademicYear, string? Description, bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, IReadOnlyList<SchoolSupplySetItemModel> Items);
public sealed record SchoolReferenceModel(Guid Id, string Name);
public sealed record SchoolSupplySetItemModel(Guid Id, Guid? ProductId, string ProductName, decimal Quantity, string Unit, string? Note);
public sealed record SaveSchoolSupplySetModel(Guid SchoolId, Guid SchoolGradeId, string Name, int AcademicYear, string? Description, bool IsActive, IReadOnlyList<SaveSchoolSupplySetItemModel>? Items = null);
public sealed record UpdateSchoolSupplySetModel(string Name, string? Description, bool IsActive);
public sealed record SaveSchoolSupplySetItemModel(Guid? ProductId, string ProductName, decimal Quantity, string Unit, string? Note);
