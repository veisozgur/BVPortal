using BV.Application.Abstractions.Authentication;
using BV.Application.Abstractions.Users;
using BV.Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace BV.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AccountController(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokens,
    IOtpService otpService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (await users.ExistsByPhoneAsync(request.Phone, cancellationToken))
        {
            return Conflict(new { message = "Bu telefon numarası zaten kayıtlı." });
        }

        var user = new User(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            passwordHasher.Hash(request.Password));

        await users.AddAsync(user, cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        await otpService.SendAsync(user.Phone, cancellationToken);

        return Created(string.Empty, new
        {
            user.Id,
            message = "Kayıt oluşturuldu. Telefon doğrulama kodu gönderildi."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.GetByPhoneAsync(request.Phone, cancellationToken);
        if (user is null || !user.IsActive || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Telefon veya şifre hatalı." });
        }

        if (!user.IsPhoneVerified)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Telefon doğrulaması gerekli." });
        }

        return Ok(new
        {
            accessToken = jwtTokens.CreateAccessToken(user, ["Customer"]),
            refreshToken = jwtTokens.CreateRefreshToken()
        });
    }

    [HttpPost("verify-phone")]
    public async Task<IActionResult> VerifyPhone(VerifyPhoneRequest request, CancellationToken cancellationToken)
    {
        if (!await otpService.VerifyAsync(request.Phone, request.Code, cancellationToken))
        {
            return BadRequest(new { message = "Kod geçersiz, süresi dolmuş veya deneme sınırı aşılmış." });
        }

        var user = await users.GetByPhoneAsync(request.Phone, cancellationToken);
        if (user is null)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı." });
        }

        user.VerifyPhone();
        await users.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Telefon başarıyla doğrulandı." });
    }

    public sealed record RegisterRequest(
        string FirstName,
        string LastName,
        string Phone,
        string Email,
        string Password);

    public sealed record LoginRequest(string Phone, string Password);
    public sealed record VerifyPhoneRequest(string Phone, string Code);
}
