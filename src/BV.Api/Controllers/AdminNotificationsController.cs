using BV.Application.Abstractions.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/notifications")]
public sealed class AdminNotificationsController(IAdminNotificationService notificationService) : ControllerBase
{
    [HttpPost("{notificationId:guid}/retry")]
    public async Task<IActionResult> Retry(Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await notificationService.RetryAsync(notificationId, cancellationToken);
        return Ok(result);
    }
}
