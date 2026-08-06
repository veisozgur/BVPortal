using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class AdminCatalogApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<IReadOnlyList<AdminCatalogCategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => await CreateClient().GetFromJsonAsync<List<AdminCatalogCategoryModel>>("api/v1/admin/catalog/categories", cancellationToken) ?? [];

    public async Task<IReadOnlyList<AdminCatalogProductModel>> GetProductsAsync(string? search = null, Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (categoryId.HasValue) query.Add($"categoryId={categoryId.Value}");
        var path = "api/v1/admin/catalog/products" + (query.Count == 0 ? string.Empty : $"?{string.Join('&', query)}");
        return await CreateClient().GetFromJsonAsync<List<AdminCatalogProductModel>>(path, cancellationToken) ?? [];
    }

    public async Task<ApiResult> CreateCategoryAsync(AdminCatalogCategoryEditModel model, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PostAsJsonAsync("api/v1/admin/catalog/categories", model, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Ok("Kategori oluşturuldu.") : ApiResult.Fail($"Kategori oluşturulamadı ({(int)response.StatusCode}).");
    }

    public async Task<ApiResult> UpdateCategoryAsync(Guid id, AdminCatalogCategoryEditModel model, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PutAsJsonAsync($"api/v1/admin/catalog/categories/{id}", model, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Ok("Kategori güncellendi.") : ApiResult.Fail($"Kategori güncellenemedi ({(int)response.StatusCode}).");
    }

    public async Task<ApiResult> CreateProductAsync(AdminCatalogProductEditModel model, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PostAsJsonAsync("api/v1/admin/catalog/products", model, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Ok("Ürün oluşturuldu.") : ApiResult.Fail($"Ürün oluşturulamadı ({(int)response.StatusCode}).");
    }

    public async Task<ApiResult> UpdateProductAsync(Guid id, AdminCatalogProductEditModel model, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().PutAsJsonAsync($"api/v1/admin/catalog/products/{id}", model, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Ok("Ürün güncellendi.") : ApiResult.Fail($"Ürün güncellenemedi ({(int)response.StatusCode}).");
    }
}

public sealed record AdminCatalogCategoryModel(Guid Id, string Name, string? Description, bool IsActive, DateTime CreatedAtUtc);
public sealed record AdminCatalogProductModel(Guid Id, Guid CategoryId, string Code, string Name, string? Brand, string Unit, decimal ListPrice, decimal VatRate, decimal StockQuantity, bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);

public sealed class AdminCatalogCategoryEditModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AdminCatalogProductEditModel
{
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string Unit { get; set; } = "Adet";
    public decimal ListPrice { get; set; }
    public decimal VatRate { get; set; } = 20;
    public decimal StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}
