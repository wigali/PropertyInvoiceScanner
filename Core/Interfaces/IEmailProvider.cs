using PropertyInvoiceScanner.Core.Models;

namespace PropertyInvoiceScanner.Core.Interfaces;

public interface IEmailProvider
{
    Task<List<EmailMessage>> GetNewEmailsAsync();
}
