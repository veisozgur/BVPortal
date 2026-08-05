using BV.Application.Abstractions.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BV.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IOtpService otpService) : ControllerBase
{
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request, CancellationToken cancellationToken)
    {
        await otpService.SendAsync(request.Phone, cancellationToken);
        return Accepted(new { message = "Doğrulama kodu gönderildi." });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        var verified = await otpService.VerifyAsync(request.Phone, request.Code, cancellationToken);
        return verified
            ? Ok(new { verified = true })
            : BadRequest(new { verified = false, message = "Kod geçersiz veya süresi dolmuş." });
    }
}

public sealed record SendOtpRequest(string Phone);
public sealed record VerifyOtpRequest(string Phone, string Code);
