using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BV.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BV.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/quote-requests")]
public sealed class QuotePdfController(BVPortalDbContext dbContext) : ControllerBase
{
    [HttpGet("{quoteRequestId:guid}/pdf")]
    public async Task<IActionResult> Download(Guid quoteRequestId, CancellationToken cancellationToken)
    {
        var quote = await dbContext.QuoteRequests
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == quoteRequestId, cancellationToken);
        if (quote is null)
            return NotFound();

        var customer = await dbContext.CustomerProfiles
            .AsNoTracking()
            .SingleAsync(x => x.Id == quote.CustomerId, cancellationToken);

        if (!User.IsInRole("Admin"))
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(userIdValue, out var userId) || customer.UserId != userId)
                return Forbid();
        }

        var response = await dbContext.QuoteResponses
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.QuoteRequestId == quoteRequestId, cancellationToken);
        if (response is null)
            return Conflict(new { message = "PDF oluşturulabilmesi için teklifin cevaplanmış olması gerekir." });

        var items = await dbContext.QuoteResponseItems
            .AsNoTracking()
            .Where(x => x.QuoteResponseId == response.Id)
            .OrderBy(x => x.ProductName)
            .ToListAsync(cancellationToken);

        var pdf = SimpleQuotePdf.Create(
            quote.Id,
            quote.Title,
            customer.FullName,
            customer.OrganizationName,
            customer.PhoneNumber,
            customer.Email,
            response.Message,
            response.ValidUntilUtc,
            items.Select(x => new PdfQuoteItem(
                x.ProductName,
                x.Quantity,
                x.Unit,
                x.UnitPrice,
                x.VatRate,
                x.Quantity * x.UnitPrice * (1 + x.VatRate / 100m))).ToList());

        return File(pdf, "application/pdf", $"BV-Teklif-{quote.Id:N}.pdf");
    }
}

internal sealed record PdfQuoteItem(
    string ProductName,
    decimal Quantity,
    string Unit,
    decimal UnitPrice,
    decimal VatRate,
    decimal LineTotal);

internal static class SimpleQuotePdf
{
    public static byte[] Create(
        Guid quoteId,
        string title,
        string customerName,
        string? organizationName,
        string phone,
        string email,
        string message,
        DateTime validUntilUtc,
        IReadOnlyList<PdfQuoteItem> items)
    {
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var lines = new List<string>
        {
            "BV KIRTASIYE - FIYAT TEKLIFI",
            $"Teklif No: {quoteId:N}",
            $"Baslik: {title}",
            $"Musteri: {customerName}",
            $"Firma: {organizationName ?? "-"}",
            $"Telefon: {phone}",
            $"E-posta: {email}",
            $"Gecerlilik: {validUntilUtc.ToLocalTime():dd.MM.yyyy}",
            "",
            message,
            "",
            "URUN | MIKTAR | BIRIM FIYAT | KDV | TOPLAM"
        };

        foreach (var item in items.Take(28))
        {
            lines.Add($"{item.ProductName} | {item.Quantity:0.##} {item.Unit} | {item.UnitPrice.ToString("N2", tr)} TL | %{item.VatRate:0.##} | {item.LineTotal.ToString("N2", tr)} TL");
        }

        if (items.Count > 28)
            lines.Add($"... {items.Count - 28} ek kalem PDF ozetinde gosterilmedi.");

        lines.Add("");
        lines.Add($"GENEL TOPLAM: {items.Sum(x => x.LineTotal).ToString("N2", tr)} TL");
        lines.Add("");
        lines.Add("Bu belge BV Portal tarafindan elektronik olarak olusturulmustur.");

        var content = BuildContent(lines);
        return BuildPdf(content);
    }

    private static string BuildContent(IEnumerable<string> lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BT");
        sb.AppendLine("/F1 10 Tf");
        sb.AppendLine("50 790 Td");

        var first = true;
        foreach (var rawLine in lines)
        {
            if (!first)
                sb.AppendLine("0 -18 Td");
            first = false;
            sb.Append('(').Append(Escape(ToAscii(rawLine))).AppendLine(") Tj");
        }

        sb.AppendLine("ET");
        return sb.ToString();
    }

    private static byte[] BuildPdf(string content)
    {
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        using var stream = new MemoryStream();
        Write(stream, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };

        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(stream.Position);
            Write(stream, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }

        var xref = stream.Position;
        Write(stream, $"xref\n0 {objects.Length + 1}\n");
        Write(stream, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1))
            Write(stream, $"{offset:0000000000} 00000 n \n");

        Write(stream, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return stream.ToArray();
    }

    private static void Write(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("(", "\\(", StringComparison.Ordinal)
        .Replace(")", "\\)", StringComparison.Ordinal);

    private static string ToAscii(string value) => value
        .Replace('ç', 'c').Replace('Ç', 'C')
        .Replace('ğ', 'g').Replace('Ğ', 'G')
        .Replace('ı', 'i').Replace('İ', 'I')
        .Replace('ö', 'o').Replace('Ö', 'O')
        .Replace('ş', 's').Replace('Ş', 'S')
        .Replace('ü', 'u').Replace('Ü', 'U')
        .Replace('₺', 'T');
}
