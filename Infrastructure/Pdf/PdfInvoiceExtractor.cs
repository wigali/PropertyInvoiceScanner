using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PropertyInvoiceScanner.Core.Models;
using UglyToad.PdfPig;

namespace PropertyInvoiceScanner.Infrastructure.Pdf;

public class PdfInvoiceExtractor
{
    private readonly ILogger<PdfInvoiceExtractor> _logger;

    private static readonly string[] InvoiceKeywords =
        ["invoice", "invoice number", "invoice date", "total", "amount due"];

    private static readonly Regex InvoiceNumberRegex =
        new(@"Invoice\s*(No|Number)?[:#]?\s*([A-Za-z0-9\-]+)", RegexOptions.IgnoreCase);

    private static readonly Regex InvoiceDateRegex =
        new(@"Invoice\s*Date[:\s]+([0-9/\-]+)", RegexOptions.IgnoreCase);

    private static readonly Regex TotalRegex =
        new(@"(Total|Amount\s*Due)[^\d]*([\d,]+\.?\d{0,2})", RegexOptions.IgnoreCase);

    public PdfInvoiceExtractor(ILogger<PdfInvoiceExtractor> logger)
    {
        _logger = logger;
    }

    public InvoiceData ExtractInvoice(string pdfPath)
    {
        var text = ExtractText(pdfPath);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("PDF has no extractable text: {Path}", pdfPath);
            return new InvoiceData { IsInvoice = false };
        }

        var isInvoice = DetectInvoice(text);
        var result = new InvoiceData { IsInvoice = isInvoice };

        if (!isInvoice)
        {
            _logger.LogInformation("Document not detected as invoice: {Path}", pdfPath);
            return result;
        }

        result.InvoiceNumber = ExtractInvoiceNumber(text);
        result.InvoiceDate = ExtractInvoiceDate(text);
        result.Total = ExtractTotal(text);

        _logger.LogInformation(
            "Extraction complete — Number: {Number}, Date: {Date}, Total: {Total}",
            result.InvoiceNumber, result.InvoiceDate, result.Total);

        return result;
    }

    private string ExtractText(string pdfPath)
    {
        var sb = new StringBuilder();

        using var document = PdfDocument.Open(pdfPath);
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }

    private bool DetectInvoice(string text)
    {
        var lower = text.ToLowerInvariant();
        int matches = InvoiceKeywords.Count(kw => lower.Contains(kw));
        return matches >= 2;
    }

    private string ExtractInvoiceNumber(string text)
    {
        var match = InvoiceNumberRegex.Match(text);
        if (!match.Success)
        {
            _logger.LogWarning("Invoice number not found in document.");
            return string.Empty;
        }
        return match.Groups[2].Value;
    }

    private DateTime? ExtractInvoiceDate(string text)
    {
        var match = InvoiceDateRegex.Match(text);
        if (!match.Success)
        {
            _logger.LogWarning("Invoice date not found in document.");
            return null;
        }

        if (DateTime.TryParse(match.Groups[1].Value, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            return date;
        }

        _logger.LogWarning("Could not parse invoice date: {Raw}", match.Groups[1].Value);
        return null;
    }

    private decimal? ExtractTotal(string text)
    {
        var match = TotalRegex.Match(text);
        if (!match.Success)
        {
            _logger.LogWarning("Invoice total not found in document.");
            return null;
        }

        var raw = match.Groups[2].Value.Replace(",", "");
        if (decimal.TryParse(raw, CultureInfo.InvariantCulture, out var total))
        {
            return total;
        }

        _logger.LogWarning("Could not parse invoice total: {Raw}", match.Groups[2].Value);
        return null;
    }
}
