using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace PayslipMgtSystem.Services.Email
{
    /// <summary>SMTP settings bound from the "EmailCred" configuration section.</summary>
    public class EmailSettings
    {
        public const string SectionName = "EmailCred";

        public string SmtpHost { get; set; } = "";
        public int Port { get; set; } = 587;
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed record EmailAttachment(string FileName, byte[] Content, string ContentType = "application/pdf");

    /// <summary>Single place that talks to the SMTP server.</summary>
    public class SmtpEmailSender
    {
        private readonly EmailSettings _settings;

        public SmtpEmailSender(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody,
            IEnumerable<EmailAttachment>? attachments = null, string? fromDisplayName = null)
        {
            using var smtpClient = new SmtpClient(_settings.SmtpHost)
            {
                Port = _settings.Port,
                Credentials = new NetworkCredential(_settings.UserName, _settings.Password),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
            };

            using var message = new MailMessage
            {
                From = fromDisplayName == null ? new MailAddress(_settings.UserName) : new MailAddress(_settings.UserName, fromDisplayName),
                Subject = subject,
                IsBodyHtml = true,
                Body = htmlBody,
            };
            message.To.Add(toEmail);

            foreach (var attachment in attachments ?? Enumerable.Empty<EmailAttachment>())
            {
                // MailMessage disposes the attachments (and their streams) when it is disposed
                message.Attachments.Add(new Attachment(new MemoryStream(attachment.Content), attachment.FileName, attachment.ContentType));
            }

            await smtpClient.SendMailAsync(message);
        }
    }
}
