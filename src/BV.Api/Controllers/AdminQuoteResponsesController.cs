using System.ComponentModel.DataAnnotations;
using BV.Application.Abstractions.Notifications;
using BV.Application.Abstractions.Quotes;
using BV.Domain.Notifications;
using BV.Domain.Quotes;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/quote-requests")]
public sealed class AdminQuoteResponsesController(
    IQuoteRequestRepository quoteRequestRepository,
    IQuoteResponseRepository quoteResponseRepository,
    ISmsSender smsSender,
    IEmailSender emailSender,
    BVPortalDbContext dbContext) : ControllerBase
{
    [HttpPost("{quoteRequestId:guid}/response")]
    public async Task<IActionResult> Answer(
        Guid quoteRequestId,
        [FromBody] AnswerQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var quoteRequest = await quoteRequestRepository.GetByIdForAdministrationAsync(quoteRequestId, cancellationToken);
        if (quoteRequest is null)
            return NotFound();

        if (await quoteResponseRepository.GetByRequestIdAsync(quoteRequestId, cancellationToken) is not null)
            return Conflict(new { message = "Bu teklif talebi daha önce cevaplandı." });

        var response = new QuoteResponse(quoteRequestId, request.Message, request.ValidUntilUtc);
        foreach (var item in request.Items)
            response.AddItem(item.ProductName, item.Quantity, item.Unit, item.UnitPrice, item.VatRate);

        response.MarkAsSent();
        quoteRequest.MarkAnswered();

        await quoteResponseRepository.AddAsync(response, cancellationToken);
        await quoteResponseRepository.SaveChangesAsync(cancellationToken);
        await quoteRequestRepository.SaveChangesAsync(cancellationToken);

        var customer = await dbContext.CustomerProfiles
            .AsNoTracking()
            .SingleAsync(x => x.Id == quoteRequest.CustomerId, cancellationToken);

        var notificationMessage = $"{quoteRequest.Title} başlıklı teklif talebiniz cevaplandı.";

        if (request.NotifyBySms)
            await SendAndRecordAsync("Sms", customer.PhoneNumber, () => smsSender.SendAsync(customer.PhoneNumber, notificationMessage, cancellationToken), quoteRequestId, cancellationToken);

        if (request.NotifyByEmail)
            await SendAndRecordAsync("Email", customer.Email, () => emailSender.SendAsync(customer.Email, "Teklifiniz cevaplandı", notificationMessage, cancellationToken), quoteRequestId, cancellationToken);

        return Ok(new
        {
            response.Id,
            response.TotalAmount,
            response.ValidUntilUtc,
            quoteRequest.Status
        });
    }

    private async Task SendAndRecordAsync(
        string channel,
        string destination,
        Func<Task> send,
        Guid quoteRequestId,
        CancellationToken cancellationToken)
    {
        var notification = new QuoteNotification(quoteRequestId, channel, destination);
        await dbContext.QuoteNotifications.AddAsync(notification, cancellationToken);

        try
        {
            await send();
            notification.MarkSent();
        }
        catch (Exception ex)
        {
            notification.MarkFailed(ex.Message);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed record AnswerQuoteRequest(
    [property: Required, MinLength(3), MaxLength(2000)] string Message,
    DateTime ValidUntilUtc,
    [property: MinLength(1)] IReadOnlyCollection<AnswerQuoteItem> Items,
    bool NotifyBySms = true,
    bool NotifyByEmail = true);

public sealed record AnswerQuoteItem(
    [property: Required, MaxLength(250)] string ProductName,
    [property: Range(0.01, 1_000_000)] decimal Quantity,
    [property: Required, MaxLength(30)] string Unit,
    [property: Range(0, 1_000_000_000)] decimal UnitPrice,
    [property: Range(0, 100)] decimal VatRate);
