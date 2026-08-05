using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class QuoteApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    public async Task<ApiResult> CreateAsync(CreateQuoteModel model, CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated || string.IsNullOrWhiteSpace(session.AccessToken))
            return ApiResult.Fail("Teklif göndermek için giriş yapmalısınız.");

        var client = httpClientFactory.CreateClient("BV.Api");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

        var payload = new
        {
            type = model.Type,
            title = model.Title,
            description = model.Description,
            items = model.Items.Select(x => new
            {
                productName = x.ProductName,
                quantity = x.Quantity,
                unit = x.Unit,
                description = x.Description
            }),
            submit = model.Submit
        };

        var response = await client.PostAsJsonAsync("api/v1/quote-requests", payload, cancellationToken);
        if (response.IsSuccessStatusCode)
            return ApiResult.Ok(model.Submit ? "Teklif talebiniz gönderildi." : "Teklif taslak olarak kaydedildi.");

        ApiMessage? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiMessage>(cancellationToken: cancellationToken);
        }
        catch
        {
            // Generic error below.
        }

        return ApiResult.Fail(error?.Message ?? $"Teklif kaydedilemedi ({(int)response.StatusCode}).");
    }
}

public sealed class CreateQuoteModel
{
    public int Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Submit { get; set; } = true;
    public List<CreateQuoteItemModel> Items { get; } = [];
}

public sealed class CreateQuoteItemModel
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public string Unit { get; set; } = "Adet";
    public string? Description { get; set; }
}
