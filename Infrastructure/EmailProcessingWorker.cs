using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PropertyInvoiceScanner.Core.Interfaces;
using PropertyInvoiceScanner.Core.Models;
using PropertyInvoiceScanner.Infrastructure.Data;
using PropertyInvoiceScanner.Infrastructure.Pdf;

namespace PropertyInvoiceScanner.Infrastructure;

public class EmailProcessingWorker : BackgroundService
{
    private readonly IEmailProvider _emailProvider;
    private readonly PdfInvoiceExtractor _pdfExtractor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailProcessingWorker> _logger;

    public EmailProcessingWorker(
        IEmailProvider emailProvider,
        PdfInvoiceExtractor pdfExtractor,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailProcessingWorker> logger)
    {
        _emailProvider = emailProvider;
        _pdfExtractor = pdfExtractor;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailProcessingWorker started");

        try
        {
            var emails = await _emailProvider.GetNewEmailsAsync();

            _logger.LogInformation($"Retrieved {emails.Count} emails");

            foreach (var email in emails)
            {
                _logger.LogInformation($"Subject: {email.Subject}");
                _logger.LogInformation($"Attachments: {email.Attachments.Count}");

                foreach (var attachment in email.Attachments)
                {
                    if (!attachment.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        continue;

                    _logger.LogInformation("Processing PDF: {FileName}", attachment.FileName);

                    var invoiceData = _pdfExtractor.ExtractInvoice(attachment.FilePath);

                    if (!invoiceData.IsInvoice)
                    {
                        _logger.LogInformation("PDF is not an invoice: {FileName}", attachment.FileName);
                        continue;
                    }

                    _logger.LogInformation(
                        "Invoice detected — Number: {Number}, Date: {Date}, Total: {Total}",
                        invoiceData.InvoiceNumber, invoiceData.InvoiceDate, invoiceData.Total);

                    await SaveProcessedEmailAsync(email, invoiceData);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving emails");
        }
    }

    private async Task SaveProcessedEmailAsync(EmailMessage email, InvoiceData invoice)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var exists = await db.ProcessedEmails
            .AnyAsync(e => e.OutlookEntryId == email.OutlookEntryId);

        if (exists)
        {
            _logger.LogInformation("Email already processed: {EntryId}", email.OutlookEntryId);
            return;
        }

        var record = new ProcessedEmail
        {
            Subject = email.Subject,
            Sender = email.Sender,
            ReceivedDate = email.ReceivedDate,
            HasAttachment = email.HasAttachments,
            IsProbableInvoice = true,
            InvoiceTotal = invoice.Total,
            ConfidenceScore = 0.9,
            ValidationStatus = "Pending",
            OutlookEntryId = email.OutlookEntryId
        };

        db.ProcessedEmails.Add(record);
        await db.SaveChangesAsync();

        _logger.LogInformation("Saved invoice to database — Subject: {Subject}, Total: {Total}",
            record.Subject, record.InvoiceTotal);
    }
}
