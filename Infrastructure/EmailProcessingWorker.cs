using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PropertyInvoiceScanner.Core.Interfaces;

namespace PropertyInvoiceScanner.Infrastructure;

public class EmailProcessingWorker : BackgroundService
{
    private readonly ILogger<EmailProcessingWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public EmailProcessingWorker(
        ILogger<EmailProcessingWorker> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailProcessingWorker started at: {Time}", DateTimeOffset.Now);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var emailProvider = scope.ServiceProvider.GetRequiredService<IEmailProvider>();

            var emails = await emailProvider.GetNewEmailsAsync();

            _logger.LogInformation("Total emails retrieved: {Count}", emails.Count);

            foreach (var email in emails)
            {
                _logger.LogInformation(
                    "Email — Subject: {Subject} | Attachments: {AttachmentCount}",
                    email.Subject,
                    email.Attachments.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving emails from Outlook.");
        }

        _logger.LogInformation("EmailProcessingWorker finished at: {Time}", DateTimeOffset.Now);
    }
}
