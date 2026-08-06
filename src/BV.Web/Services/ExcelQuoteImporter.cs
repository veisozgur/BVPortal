using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components.Forms;

namespace BV.Web.Services;

public sealed class ExcelQuoteImporter
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly XNamespace SpreadsheetNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace RelationshipsNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipsNs = "http://schemas.openxmlformats.org/package/2006/relationships";

    public async Task<ExcelImportResult> ImportAsync(IBrowserFile file, CancellationToken cancellationToken = default)
    {
        if (!file.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return ExcelImportResult.Fail("Yalnızca .xlsx uzantılı Excel dosyaları desteklenir.");

        await using var input = file.OpenReadStream(MaxFileSize, cancellationToken);
        await using var buffer = new MemoryStream();
        await input.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        try
        {
            using var archive = new ZipArchive(buffer, ZipArchiveMode.Read, leaveOpen: false);
            var sharedStrings = ReadSharedStrings(archive);
            var sheetPath = ResolveFirstWorksheetPath(archive);
            if (sheetPath is null)
                return ExcelImportResult.Fail("Excel dosyasında çalışma sayfası bulunamadı.");

            var sheetEntry = archive.GetEntry(sheetPath);
            if (sheetEntry is null)
                return ExcelImportResult.Fail("Excel çalışma sayfası okunamadı.");

            using var sheetStream = sheetEntry.Open();
            var sheet = XDocument.Load(sheetStream);
            var rows = sheet.Descendants(SpreadsheetNs + "row").ToList();
            if (rows.Count < 2)
                return ExcelImportResult.Fail("Excel dosyasında başlık dışında ürün satırı bulunamadı.");

            var headers = ReadRow(rows[0], sharedStrings)
                .Select((value, index) => new { Key = NormalizeHeader(value), Index = index })
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.First().Index);

            var productIndex = FindHeader(headers, "urun", "urunadi", "malzeme", "aciklama");
            var quantityIndex = FindHeader(headers, "miktar", "adet", "quantity");
            var unitIndex = FindHeader(headers, "birim", "unit");
            var notesIndex = FindHeader(headers, "not", "notlar", "detay", "aciklama");

            if (productIndex < 0 || quantityIndex < 0)
                return ExcelImportResult.Fail("Excel başlıklarında en az 'Ürün' ve 'Miktar' sütunları bulunmalıdır.");

            var items = new List<CreateQuoteItemModel>();
            var errors = new List<string>();

            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var values = ReadRow(rows[rowIndex], sharedStrings);
                var productName = ValueAt(values, productIndex).Trim();
                if (string.IsNullOrWhiteSpace(productName))
                    continue;

                var quantityText = ValueAt(values, quantityIndex).Trim();
                if (!TryParseDecimal(quantityText, out var quantity) || quantity <= 0)
                {
                    errors.Add($"{rowIndex + 1}. satır: Miktar geçersiz.");
                    continue;
                }

                items.Add(new CreateQuoteItemModel
                {
                    ProductName = productName,
                    Quantity = quantity,
                    Unit = unitIndex >= 0 && !string.IsNullOrWhiteSpace(ValueAt(values, unitIndex))
                        ? ValueAt(values, unitIndex).Trim()
                        : "Adet",
                    Description = notesIndex >= 0 ? NullIfEmpty(ValueAt(values, notesIndex)) : null
                });
            }

            if (items.Count == 0)
                return ExcelImportResult.Fail(errors.Count > 0
                    ? string.Join(" ", errors.Take(5))
                    : "Aktarılabilecek geçerli ürün satırı bulunamadı.");

            var message = $"{items.Count} ürün kalemi Excel dosyasından aktarıldı.";
            if (errors.Count > 0)
                message += $" {errors.Count} hatalı satır atlandı: {string.Join(" ", errors.Take(3))}";

            return ExcelImportResult.Ok(items, message);
        }
        catch (InvalidDataException)
        {
            return ExcelImportResult.Fail("Dosya geçerli bir .xlsx çalışma kitabı değil.");
        }
        catch (Exception ex)
        {
            return ExcelImportResult.Fail($"Excel dosyası okunamadı: {ex.Message}");
        }
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
            return [];

        using var stream = entry.Open();
        var document = XDocument.Load(stream);
        return document.Descendants(SpreadsheetNs + "si")
            .Select(si => string.Concat(si.Descendants(SpreadsheetNs + "t").Select(x => x.Value)))
            .ToList();
    }

    private static string? ResolveFirstWorksheetPath(ZipArchive archive)
    {
        var workbookEntry = archive.GetEntry("xl/workbook.xml");
        var relationshipsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels");
        if (workbookEntry is null || relationshipsEntry is null)
            return null;

        using var workbookStream = workbookEntry.Open();
        using var relationshipsStream = relationshipsEntry.Open();
        var workbook = XDocument.Load(workbookStream);
        var relationships = XDocument.Load(relationshipsStream);

        var firstSheet = workbook.Descendants(SpreadsheetNs + "sheet").FirstOrDefault();
        var relationshipId = firstSheet?.Attribute(RelationshipsNs + "id")?.Value;
        if (relationshipId is null)
            return null;

        var target = relationships.Descendants(PackageRelationshipsNs + "Relationship")
            .FirstOrDefault(x => x.Attribute("Id")?.Value == relationshipId)
            ?.Attribute("Target")?.Value;

        if (string.IsNullOrWhiteSpace(target))
            return null;

        var normalized = target.Replace('\\', '/').TrimStart('/');
        return normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase) ? normalized : $"xl/{normalized}";
    }

    private static List<string> ReadRow(XElement row, IReadOnlyList<string> sharedStrings)
    {
        var cells = new SortedDictionary<int, string>();
        foreach (var cell in row.Elements(SpreadsheetNs + "c"))
        {
            var reference = cell.Attribute("r")?.Value ?? string.Empty;
            var columnIndex = ColumnIndex(reference);
            var type = cell.Attribute("t")?.Value;
            var value = cell.Element(SpreadsheetNs + "v")?.Value ?? string.Empty;

            if (type == "s" && int.TryParse(value, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                value = sharedStrings[sharedIndex];
            else if (type == "inlineStr")
                value = string.Concat(cell.Descendants(SpreadsheetNs + "t").Select(x => x.Value));

            cells[columnIndex] = value;
        }

        if (cells.Count == 0)
            return [];

        var result = Enumerable.Repeat(string.Empty, cells.Keys.Max() + 1).ToList();
        foreach (var pair in cells)
            result[pair.Key] = pair.Value;
        return result;
    }

    private static int ColumnIndex(string cellReference)
    {
        var letters = new string(cellReference.TakeWhile(char.IsLetter).ToArray()).ToUpperInvariant();
        var index = 0;
        foreach (var letter in letters)
            index = index * 26 + letter - 'A' + 1;
        return Math.Max(0, index - 1);
    }

    private static int FindHeader(IReadOnlyDictionary<string, int> headers, params string[] candidates)
    {
        foreach (var candidate in candidates)
            if (headers.TryGetValue(candidate, out var index))
                return index;
        return -1;
    }

    private static string NormalizeHeader(string value)
    {
        var normalized = value.Trim().ToLowerInvariant()
            .Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g')
            .Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');
        return new string(normalized.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string ValueAt(IReadOnlyList<string> values, int index)
        => index >= 0 && index < values.Count ? values[index] : string.Empty;

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryParseDecimal(string value, out decimal result)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out result)
           || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
}

public sealed record ExcelImportResult(bool Success, IReadOnlyList<CreateQuoteItemModel> Items, string Message)
{
    public static ExcelImportResult Ok(IReadOnlyList<CreateQuoteItemModel> items, string message) => new(true, items, message);
    public static ExcelImportResult Fail(string message) => new(false, [], message);
}
