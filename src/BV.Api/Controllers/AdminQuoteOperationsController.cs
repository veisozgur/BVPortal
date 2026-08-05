using BV.Application.Abstractions.Admin;
using BV.Domain.Quotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/quote-requests")]
public sealed class AdminQuoteOperationsController(IAdminQuoteOperations operations) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var detail = await operations.GetDetailAsync(id, cancellationToken);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeAdminQuoteStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await operations.ChangeStatusAsync(id, request.Status, cancellationToken);
        return updated ? NoContent() : NotFound();
    }
}

public sealed record ChangeAdminQuoteStatusRequest(QuoteRequestStatus Status);
