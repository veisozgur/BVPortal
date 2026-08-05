using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using BV.Application.Abstractions.Authentication;
using BV.Application.Abstractions.Users;
using BV.Domain.Authentication;
using BV.Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace BV.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AccountController(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokens,
    IRefreshTokenRepository refreshTokens,
    IOtpService otpService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (await users.ExistsByPhoneAsync(request.Phone, cancellationToken))
            return Conflict(new { message = "Bu telefon numarası zaten kayıtlı." });

        var user = new User(request.FirstName, request.LastName, request.Phone, request.Email, passwordHasher.Hash(request.Password));
        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        await otpService.SendAsync(user.Phone, cancellationToken);

        return Created(string.Empty, new { user.Id, message = "Kayıt oluşturuldu. Telefon doğrulama kodu gönderildi." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.GetByPhoneAsync(request.Phone, cancellationToken);
        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Telefon veya şifre hatalı." });

        if (!user.IsPhoneVerified)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Telefon doğrulaması gerekli." });

        var rawRefreshToken = jwtTokens.CreateRefreshToken();
        await refreshTokens.AddAsync(new RefreshToken(user.Id, HashToken(rawRefreshToken), DateTime.UtcNow.AddDays(30)), cancellationToken);
        await refreshTokens.SaveChangesAsync(cancellationToken);
        return Ok(CreateTokenResponse(user, rawRefreshToken));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        var storedToken = await refreshTokens.GetActiveByHashAsync(HashToken(request.RefreshToken), cancellationToken);
        if (storedToken is null || !storedToken.IsActive(DateTime.UtcNow))
            return Unauthorized(new { message = "Refresh token geçersiz veya süresi dolmuş." });

        var user = await users.GetByIdAsync(storedToken.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "Kullanıcı hesabı aktif değil." });

        storedToken.Revoke(DateTime.UtcNow);
        var newRawToken = jwtTokens.CreateRefreshToken();
        await refreshTokens.AddAsync(new RefreshToken(user.Id, HashToken(newRawToken), DateTime.UtcNow.AddDays(30)), cancellationToken);
        await refreshTokens.SaveChangesAsync(cancellationToken);
        return Ok(CreateTokenResponse(user, newRawToken));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        var storedToken = await refreshTokens.GetActiveByHashAsync(HashToken(request.RefreshToken), cancellationToken);
        if (storedToken is not null)
        {
            storedToken.Revoke(DateTime.UtcNow);
            await refreshTokens.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }

    [HttpPost("verify-phone")]
    public async Task<IActionResult> VerifyPhone(VerifyPhoneRequest request, CancellationToken cancellationToken)
    {
        if (!await otpService.VerifyAsync(request.Phone, request.Code, cancellationToken))
            return BadRequest(new { message = "Kod geçersiz, süresi dolmuş veya deneme sınırı aşılmış." });

        var user = await users.GetByPhoneAsync(request.Phone, cancellationToken);
        if (user is null)
            return NotFound(new { message = "Kullanıcı bulunamadı." });

        user.VerifyPhone();
        await users.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Telefon başarıyla doğrulandı." });
    }

    private object CreateTokenResponse(User user, string refreshToken) => new
    {
        accessToken = jwtTokens.CreateAccessToken(user, [user.Role.ToString()]),
        refreshToken,
        expiresIn = 900,
        role = user.Role.ToString()
    };

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public sealed record RegisterRequest(
        [property: Required, StringLength(100, MinimumLength = 2)] string FirstName,
        [property: Required, StringLength(100, MinimumLength = 2)] string LastName,
        [property: Required, RegularExpression(@"^(\+?90|0)?5\d{9}$")] string Phone,
        [property: Required, EmailAddress, StringLength(256)] string Email,
        [property: Required, StringLength(100, MinimumLength = 8)] string Password);

    public sealed record LoginRequest(
        [property: Required, RegularExpression(@"^(\+?90|0)?5\d{9}$")] string Phone,
        [property: Required] string Password);

    public sealed record RefreshRequest([property: Required, MinLength(32)] string RefreshToken);

    public sealed record VerifyPhoneRequest(
        [property: Required, RegularExpression(@"^(\+?90|0)?5\d{9}$")] string Phone,
        [property: Required, RegularExpression(@"^\d{6}$")] string Code);
}
