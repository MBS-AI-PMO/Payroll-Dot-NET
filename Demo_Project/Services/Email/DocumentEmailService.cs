using PayslipMgtSystem.Services.Pdf;
using PayslipMgtSystem.Services.Pdf.Templates;
using PayslipMgtSystem.ViewModel;

namespace PayslipMgtSystem.Services.Email
{
    /// <summary>Emails payslips and Income Tax Certificates to employees as PDF attachments.</summary>
    public class DocumentEmailService
    {
        private const string PayslipSubject = "Payroll Details";
        private const string PayslipBody = "Please find the attached payroll details in PDF format.";
        private const string ItcSubject = "ITC Details";
        private const string ItcBody = "Please find the attached ITC details in PDF format.";

        private readonly SmtpEmailSender _sender;
        private readonly PdfRenderer _pdfRenderer;
        private readonly ILogger<DocumentEmailService> _logger;

        public DocumentEmailService(SmtpEmailSender sender, PdfRenderer pdfRenderer, ILogger<DocumentEmailService> logger)
        {
            _sender = sender;
            _pdfRenderer = pdfRenderer;
            _logger = logger;
        }

        /// <summary>Emails one payslip. Returns false (and logs why) when it could not be sent.</summary>
        public Task<bool> SendPayslipAsync(UploadFileVM file, PdfLayout layout, TimeSpan? delayBeforeSend = null) =>
            SendAsync(file.Email, PayslipSubject, PayslipBody,
                () => PayslipTemplate.Render(file), PayslipTemplate.FileName(file), layout, delayBeforeSend);

        /// <summary>Emails one Income Tax Certificate. Returns false (and logs why) when it could not be sent.</summary>
        public Task<bool> SendItcAsync(UploadFileVM file, int startYear, PdfLayout layout, TimeSpan? delayBeforeSend = null) =>
            SendAsync(file.Email, ItcSubject, ItcBody,
                () => ItcTemplate.Render(file, startYear), ItcTemplate.FileName(file, startYear), layout, delayBeforeSend);

        private async Task<bool> SendAsync(string? toEmail, string subject, string body, Func<string> renderHtml,
            string fileName, PdfLayout layout, TimeSpan? delayBeforeSend)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Skipped '{Subject}' email ({File}): no email address", subject, fileName);
                return false;
            }

            try
            {
                var pdf = _pdfRenderer.Render(renderHtml(), layout);
                if (delayBeforeSend.HasValue)
                    await Task.Delay(delayBeforeSend.Value);   // keeps bulk sends under the SMTP provider's rate limit
                await _sender.SendAsync(toEmail, subject, body, new[] { new EmailAttachment(fileName, pdf) });
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send '{Subject}' email to {Email}", subject, toEmail);
                return false;
            }
        }
    }
}
