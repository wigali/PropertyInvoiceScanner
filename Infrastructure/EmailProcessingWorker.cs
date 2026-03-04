using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PropertyInvoiceScanner.Core.Interfaces;

namespace PropertyInvoiceScanner.Infrastructure;

public class EmailProcessingWorker : BackgroundService
{
    private readonly IEmailProvider _emailProvider;
    private readonly ILogger<EmailProcessingWorker> _logger;

    public EmailProcessingWorker(
        IEmailProvider emailProvider,
        ILogger<EmailProcessingWorker> logger)
    {
        _emailProvider = emailProvider;
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
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving emails");
        }
    }
}
