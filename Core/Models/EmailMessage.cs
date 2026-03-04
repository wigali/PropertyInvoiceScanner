namespace PropertyInvoiceScanner.Core.Models;

public class EmailMessage
{
    public string Subject { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public bool HasAttachments { get; set; }
    public string OutlookEntryId { get; set; } = string.Empty;
    public List<EmailAttachment> Attachments { get; set; } = new();
}
