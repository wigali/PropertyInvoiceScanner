namespace PropertyInvoiceScanner.Core.Models;

public class ProcessedEmail
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public bool HasAttachment { get; set; }
    public bool IsProbableInvoice { get; set; }
    public decimal? InvoiceTotal { get; set; }
    public double ConfidenceScore { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
    public string OutlookEntryId { get; set; } = string.Empty;
}
