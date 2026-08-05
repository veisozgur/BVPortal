using BV.Application.Abstractions.Admin;
using BV.Domain.Quotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public sealed class AdminDashboardController(IAdminDashboardQuery dashboardQuery) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var summary = await dashboardQuery.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    [HttpGet("dashboard/daily-metrics")]
    public async Task<IActionResult> GetDailyMetrics(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var metrics = await dashboardQuery.GetDailyMetricsAsync(days, cancellationToken);
        return Ok(metrics);
    }

    [HttpGet("quote-requests")]
    public async Task<IActionResult> ListQuoteRequests(
        [FromQuery] QuoteRequestStatus? status,
        [FromQuery] QuoteRequestType? type,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var quotes = await dashboardQuery.ListQuotesAsync(
            status,
            type,
            search,
            page,
            pageSize,
            cancellationToken);

        return Ok(quotes);
    }
}
