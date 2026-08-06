using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class ProfileApiClient(IHttpClientFactory factory, AuthSession session)
{
    private HttpClient Client()
    {
        var client = factory.CreateClient("BV.Api");
        if (!string.IsNullOrWhiteSpace(session.AccessToken))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        return client;
    }

    public async Task<CustomerProfileModel?> GetAsync(CancellationToken cancellationToken = default)
    {
        var response = await Client().GetAsync("api/v1/customer-profile", cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<CustomerProfileModel>(cancellationToken: cancellationToken)
            : null;
    }

    public async Task<ApiResult> CreateAsync(CustomerProfileModel model, CancellationToken cancellationToken = default)
    {
        var response = await Client().PostAsJsonAsync("api/v1/customer-profile", model, cancellationToken);
        return response.IsSuccessStatusCode ? ApiResult.Ok("Profil kaydedildi.") : ApiResult.Fail($"Profil kaydedilemedi ({(int)response.StatusCode}).");
    }
}

public sealed class CustomerProfileModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? OrganizationName { get; set; }
    public string? TaxNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
}
