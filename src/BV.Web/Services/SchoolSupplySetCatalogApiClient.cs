using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class SchoolSupplySetCatalogApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<IReadOnlyList<SchoolOptionModel>> SchoolsAsync(CancellationToken cancellationToken = default) =>
        !session.IsAuthenticated
            ? []
            : await CreateClient().GetFromJsonAsync<List<SchoolOptionModel>>("api/v1/school-supply-sets/schools", cancellationToken) ?? [];

    public async Task<IReadOnlyList<SchoolGradeOptionModel>> GradesAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        !session.IsAuthenticated || schoolId == Guid.Empty
            ? []
            : await CreateClient().GetFromJsonAsync<List<SchoolGradeOptionModel>>($"api/v1/school-supply-sets/schools/{schoolId}/grades", cancellationToken) ?? [];

    public async Task<IReadOnlyList<SchoolSupplySetOptionModel>> SetsAsync(Guid schoolId, Guid gradeId, CancellationToken cancellationToken = default) =>
        !session.IsAuthenticated || schoolId == Guid.Empty || gradeId == Guid.Empty
            ? []
            : await CreateClient().GetFromJsonAsync<List<SchoolSupplySetOptionModel>>(
                $"api/v1/school-supply-sets?schoolId={schoolId}&gradeId={gradeId}", cancellationToken) ?? [];

    public async Task<SchoolSupplySetCatalogDetailModel?> GetAsync(Guid setId, CancellationToken cancellationToken = default) =>
        !session.IsAuthenticated || setId == Guid.Empty
            ? null
            : await CreateClient().GetFromJsonAsync<SchoolSupplySetCatalogDetailModel>($"api/v1/school-supply-sets/{setId}", cancellationToken);
}

public sealed record SchoolOptionModel(Guid Id, string Name, string? Code);
public sealed record SchoolGradeOptionModel(Guid Id, string Name);
public sealed record SchoolSupplySetOptionModel(Guid Id, string Name, int AcademicYear, string? Description, int ItemCount);
public sealed record SchoolSupplySetCatalogDetailModel(
    Guid Id,
    Guid SchoolId,
    Guid SchoolGradeId,
    string Name,
    int AcademicYear,
    string? Description,
    IReadOnlyList<SchoolSupplySetQuoteItemModel> Items);
public sealed record SchoolSupplySetQuoteItemModel(string ProductName, decimal Quantity, string Unit, string? Description);
