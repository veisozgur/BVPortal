using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class CatalogApiClient(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("BV.Api");

    public async Task<IReadOnlyList<CatalogCategoryModel>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => await Client.GetFromJsonAsync<List<CatalogCategoryModel>>("api/v1/catalog/categories", cancellationToken) ?? [];

    public async Task<CatalogProductPage> GetProductsAsync(
        string? search = null,
        Guid? categoryId = null,
        int page = 1,
        int pageSize = 24,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"page={Math.Max(1, page)}",
            $"pageSize={Math.Clamp(pageSize, 1, 100)}"
        };

        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search.Trim())}");
        if (categoryId.HasValue)
            query.Add($"categoryId={categoryId.Value}");

        return await Client.GetFromJsonAsync<CatalogProductPage>(
            $"api/v1/catalog/products?{string.Join('&', query)}",
            cancellationToken) ?? new CatalogProductPage([], 0, page, pageSize);
    }
}

public sealed record CatalogCategoryModel(Guid Id, string Name, string? Description);
public sealed record CatalogProductModel(
    Guid Id,
    Guid CategoryId,
    string Code,
    string Name,
    string? Brand,
    string Unit,
    decimal ListPrice,
    decimal VatRate,
    bool InStock);
public sealed record CatalogProductPage(
    IReadOnlyList<CatalogProductModel> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed class CatalogSelection
{
    private readonly Dictionary<Guid, CatalogSelectionItem> items = [];
    public IReadOnlyCollection<CatalogSelectionItem> Items => items.Values;
    public int Count => items.Count;

    public void Add(CatalogProductModel product)
    {
        if (items.TryGetValue(product.Id, out var existing))
        {
            existing.Quantity += 1;
            return;
        }

        items[product.Id] = new CatalogSelectionItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Unit = product.Unit,
            Quantity = 1,
            Description = string.IsNullOrWhiteSpace(product.Brand) ? product.Code : $"{product.Brand} - {product.Code}"
        };
    }

    public void Remove(Guid productId) => items.Remove(productId);
    public void Clear() => items.Clear();
}

public sealed class CatalogSelectionItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = "Adet";
    public decimal Quantity { get; set; } = 1;
    public string? Description { get; set; }
}
