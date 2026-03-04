using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using PropertyInvoiceScanner.Core.Interfaces;
using PropertyInvoiceScanner.Core.Models;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace PropertyInvoiceScanner.Infrastructure.Email;

public class OutlookEmailProvider : IEmailProvider
{
    private readonly ILogger<OutlookEmailProvider> _logger;

    public OutlookEmailProvider(ILogger<OutlookEmailProvider> logger)
    {
        _logger = logger;
    }

    public Task<List<EmailMessage>> GetNewEmailsAsync()
    {
        var messages = new List<EmailMessage>();

        Outlook.Application? outlookApp = null;
        Outlook.NameSpace? ns = null;
        Outlook.MAPIFolder? inbox = null;
        Outlook.MAPIFolder? folder = null;
        Outlook.Items? items = null;
        Outlook.Items? filtered = null;

        try
        {
            outlookApp = new Outlook.Application();
            ns = outlookApp.GetNamespace("MAPI");
            inbox = ns.GetDefaultFolder(Outlook.OlDefaultFolders.olFolderInbox);

            try
            {
                folder = inbox.Folders["ParaProcesar"];
            }
            catch (COMException)
            {
                throw new InvalidOperationException(
                    "Subfolder 'ParaProcesar' not found inside Inbox.");
            }

            items = folder.Items;
            filtered = items.Restrict("[Unread] = true");

            _logger.LogInformation("Found {Count} unread emails in ParaProcesar.", filtered.Count);

            foreach (object item in filtered)
            {
                if (item is not Outlook.MailItem mail)
                {
                    ReleaseComObject(item);
                    continue;
                }

                try
                {
                    if (mail.Attachments.Count == 0)
                        continue;

                    var message = new EmailMessage
                    {
                        Subject = mail.Subject ?? string.Empty,
                        Sender = mail.SenderEmailAddress ?? string.Empty,
                        ReceivedDate = mail.ReceivedTime,
                        HasAttachments = true,
                        OutlookEntryId = mail.EntryID
                    };

                    Outlook.Attachments? attachments = null;
                    try
                    {
                        attachments = mail.Attachments;
                        for (int i = 1; i <= attachments.Count; i++)
                        {
                            Outlook.Attachment? att = null;
                            try
                            {
                                att = attachments[i];
                                var tempPath = Path.Combine(Path.GetTempPath(), att.FileName);
                                att.SaveAsFile(tempPath);

                                message.Attachments.Add(new EmailAttachment
                                {
                                    FileName = att.FileName,
                                    FilePath = tempPath
                                });

                                _logger.LogInformation("Saved attachment: {FileName}", att.FileName);
                            }
                            finally
                            {
                                ReleaseComObject(att);
                            }
                        }
                    }
                    finally
                    {
                        ReleaseComObject(attachments);
                    }

                    messages.Add(message);
                }
                finally
                {
                    ReleaseComObject(mail);
                }
            }
        }
        finally
        {
            ReleaseComObject(filtered);
            ReleaseComObject(items);
            ReleaseComObject(folder);
            ReleaseComObject(inbox);
            ReleaseComObject(ns);
            ReleaseComObject(outlookApp);
        }

        return Task.FromResult(messages);
    }

    private static void ReleaseComObject(object? obj)
    {
        if (obj is null) return;
        try { Marshal.ReleaseComObject(obj); }
        catch { /* best-effort cleanup */ }
    }
}
