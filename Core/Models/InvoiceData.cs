namespace PropertyInvoiceScanner.Core.Models;

public class InvoiceData
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime? InvoiceDate { get; set; }
    public decimal? Total { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public bool IsInvoice { get; set; }
}
