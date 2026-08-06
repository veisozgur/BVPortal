using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class OrderApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<IReadOnlyList<CustomerOrderListItemModel>> ListAsync(CancellationToken cancellationToken = default) =>
        !session.IsAuthenticated
            ? []
            : await CreateClient().GetFromJsonAsync<List<CustomerOrderListItemModel>>("api/v1/orders", cancellationToken) ?? [];

    public async Task<CustomerOrderDetailModel?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        !session.IsAuthenticated
            ? null
            : await CreateClient().GetFromJsonAsync<CustomerOrderDetailModel>($"api/v1/orders/{orderId}", cancellationToken);
}

public sealed record CustomerOrderListItemModel(
    Guid Id,
    string OrderNumber,
    Guid QuoteRequestId,
    int Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? CompletedAtUtc,
    int ItemCount,
    decimal TotalAmount);

public sealed record CustomerOrderDetailModel(
    Guid Id,
    string OrderNumber,
    Guid QuoteRequestId,
    int Status,
    string? CustomerNote,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? CompletedAtUtc,
    decimal TotalAmount,
    IReadOnlyList<CustomerOrderItemModel> Items);

public sealed record CustomerOrderItemModel(
    Guid Id,
    string ProductName,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal);
