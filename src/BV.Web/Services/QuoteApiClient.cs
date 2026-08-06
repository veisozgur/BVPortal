using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class QuoteApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<ApiResult> CreateAsync(CreateQuoteModel model, CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
            return ApiResult.Fail("Teklif göndermek için giriş yapmalısınız.");

        var payload = new
        {
            type = model.Type,
            title = model.Title,
            description = model.Description,
            items = model.Items.Select(x => new { productName = x.ProductName, quantity = x.Quantity, unit = x.Unit, description = x.Description }),
            submit = model.Submit
        };

        var response = await CreateClient().PostAsJsonAsync("api/v1/quote-requests", payload, cancellationToken);
        return response.IsSuccessStatusCode
            ? ApiResult.Ok(model.Submit ? "Teklif talebiniz gönderildi." : "Teklif taslak olarak kaydedildi.")
            : ApiResult.Fail($"Teklif kaydedilemedi ({(int)response.StatusCode}).");
    }

    public async Task<IReadOnlyList<QuoteSummaryModel>> ListAsync(CancellationToken cancellationToken = default)
        => await CreateClient().GetFromJsonAsync<List<QuoteSummaryModel>>("api/v1/quote-requests", cancellationToken) ?? [];

    public async Task<QuoteDetailModel?> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await CreateClient().GetFromJsonAsync<QuoteDetailModel>($"api/v1/quote-requests/{id}", cancellationToken);

    public async Task<QuoteResponseModel?> GetResponseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().GetAsync($"api/v1/quote-requests/{id}/response", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<QuoteResponseModel>(cancellationToken: cancellationToken)
            : null;
    }

    public async Task<PdfDownloadResult> DownloadPdfAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await CreateClient().GetAsync($"api/v1/quote-requests/{id}/pdf", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new PdfDownloadResult(false, null, null, $"PDF indirilemedi ({(int)response.StatusCode}).");

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? $"BV-Teklif-{id:N}.pdf";

        return new PdfDownloadResult(true, bytes, fileName, null);
    }
}

public sealed class CreateQuoteModel { public int Type { get; set; } public string Title { get; set; } = string.Empty; public string? Description { get; set; } public bool Submit { get; set; } = true; public List<CreateQuoteItemModel> Items { get; } = []; }
public sealed class CreateQuoteItemModel { public string ProductName { get; set; } = string.Empty; public decimal Quantity { get; set; } = 1; public string Unit { get; set; } = "Adet"; public string? Description { get; set; } }
public sealed record QuoteSummaryModel(Guid Id, int Type, int Status, string Title, DateTime CreatedAtUtc, DateTime? SubmittedAtUtc, DateTime? AnsweredAtUtc, List<QuoteItemModel> Items);
public sealed record QuoteDetailModel(Guid Id, int Type, int Status, string Title, string? Description, DateTime CreatedAtUtc, DateTime? SubmittedAtUtc, DateTime? AnsweredAtUtc, List<QuoteItemModel> Items);
public sealed record QuoteItemModel(Guid Id, string ProductName, decimal Quantity, string Unit, string? Notes);
public sealed record QuoteResponseModel(Guid Id, Guid QuoteRequestId, DateTime ValidUntilUtc, string? Notes, DateTime? SentAtUtc, List<QuoteResponseItemModel> Items);
public sealed record QuoteResponseItemModel(Guid Id, string ProductName, decimal Quantity, string Unit, decimal UnitPrice, decimal VatRate, decimal LineTotal);
public sealed record PdfDownloadResult(bool Success, byte[]? Content, string? FileName, string? Error);
