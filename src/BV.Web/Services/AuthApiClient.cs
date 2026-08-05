using System.Net.Http.Json;

namespace BV.Web.Services;

public sealed class AuthApiClient(IHttpClientFactory httpClientFactory, AuthSession session)
{
    private HttpClient Client => httpClientFactory.CreateClient("BV.Api");

    public async Task<ApiResult> RegisterAsync(RegisterModel model, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("api/v1/auth/register", model, cancellationToken);
        return await ToResultAsync(response, cancellationToken);
    }

    public async Task<ApiResult> VerifyPhoneAsync(string phone, string code, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("api/v1/auth/verify-phone", new { phone, code }, cancellationToken);
        return await ToResultAsync(response, cancellationToken);
    }

    public async Task<ApiResult> SendOtpAsync(string phone, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("api/v1/auth/send-otp", new { phone }, cancellationToken);
        return await ToResultAsync(response, cancellationToken);
    }

    public async Task<ApiResult> LoginAsync(LoginModel model, CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsJsonAsync("api/v1/auth/login", model, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return await ToResultAsync(response, cancellationToken);

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        if (token is null)
            return ApiResult.Fail("Sunucudan geçerli oturum bilgisi alınamadı.");

        session.SetTokens(token.AccessToken, token.RefreshToken, token.ExpiresIn);
        return ApiResult.Ok("Giriş başarılı.");
    }

    private static async Task<ApiResult> ToResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ApiMessage? body = null;
        try
        {
            body = await response.Content.ReadFromJsonAsync<ApiMessage>(cancellationToken: cancellationToken);
        }
        catch
        {
            // Non-JSON error responses are represented with a generic message.
        }

        return response.IsSuccessStatusCode
            ? ApiResult.Ok(body?.Message ?? "İşlem başarılı.")
            : ApiResult.Fail(body?.Message ?? $"İşlem başarısız oldu ({(int)response.StatusCode}).");
    }
}

public sealed class AuthSession
{
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken) && ExpiresAt > DateTimeOffset.UtcNow;

    public void SetTokens(string accessToken, string refreshToken, int expiresIn)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
    }

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        ExpiresAt = null;
    }
}

public sealed record RegisterModel(string FirstName, string LastName, string Phone, string Email, string Password);
public sealed record LoginModel(string Phone, string Password);
public sealed record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn);
public sealed record ApiMessage(string? Message);
public sealed record ApiResult(bool Success, string Message)
{
    public static ApiResult Ok(string message) => new(true, message);
    public static ApiResult Fail(string message) => new(false, message);
}
