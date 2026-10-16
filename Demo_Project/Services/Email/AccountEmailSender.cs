using System.Text.Encodings.Web;

namespace PayslipMgtSystem.Services.Email
{
    /// <summary>
    /// Sends account-related emails (invitations, password resets).
    /// </summary>
    public class AccountEmailSender
    {
        public const string PortalName = "NITB Payslip Portal";

        private readonly SmtpEmailSender _sender;

        public AccountEmailSender(SmtpEmailSender sender)
        {
            _sender = sender;
        }

        public Task SendInvitationAsync(string toEmail, string? name, string setPasswordLink)
        {
            var body = BuildTemplate(
                heading: "Your payslip account is ready",
                greeting: $"Dear {Encode(name ?? "Employee")},",
                intro: $"An account has been created for you on the <strong>{PortalName}</strong>. " +
                       "You can use it to view and download your monthly payslips at any time.",
                instruction: "To get started, click the button below and choose a password for your account.",
                buttonText: "Set My Password",
                link: setPasswordLink,
                footnote: $"Your sign-in email is <strong>{Encode(toEmail)}</strong>. This link expires in 3 days. " +
                          "If it has expired, use <em>Forgot Password</em> on the sign-in page to request a new one.");

            return SendAsync(toEmail, $"Welcome to the {PortalName} – set your password", body);
        }

        public Task SendPasswordResetAsync(string toEmail, string? name, string resetLink)
        {
            var body = BuildTemplate(
                heading: "Reset your password",
                greeting: $"Dear {Encode(name ?? "User")},",
                intro: $"We received a request to reset the password for your <strong>{PortalName}</strong> account.",
                instruction: "Click the button below to choose a new password.",
                buttonText: "Reset Password",
                link: resetLink,
                footnote: "If you did not request a password reset, you can safely ignore this email – your password will not change.");

            return SendAsync(toEmail, $"{PortalName} – password reset request", body);
        }

        private Task SendAsync(string toEmail, string subject, string htmlBody) =>
            _sender.SendAsync(toEmail, subject, htmlBody, fromDisplayName: PortalName);

        private static string Encode(string value) => HtmlEncoder.Default.Encode(value);

        private static string BuildTemplate(string heading, string greeting, string intro, string instruction,
            string buttonText, string link, string footnote)
        {
            var safeLink = HtmlEncoder.Default.Encode(link);
            return $@"<!DOCTYPE html>
<html>
<body style=""margin:0;padding:0;background:#f1f4f9;font-family:Segoe UI,Arial,sans-serif;color:#1f2937;"">
  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""background:#f1f4f9;padding:32px 12px;"">
    <tr><td align=""center"">
      <table role=""presentation"" width=""560"" cellspacing=""0"" cellpadding=""0"" style=""max-width:560px;width:100%;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e3e8f0;"">
        <tr><td style=""background:#0f2a65;padding:22px 32px;color:#ffffff;font-size:18px;font-weight:600;"">{PortalName}</td></tr>
        <tr><td style=""padding:32px;"">
          <h1 style=""margin:0 0 16px;font-size:22px;color:#0f2a65;"">{heading}</h1>
          <p style=""margin:0 0 12px;font-size:15px;line-height:1.6;"">{greeting}</p>
          <p style=""margin:0 0 12px;font-size:15px;line-height:1.6;"">{intro}</p>
          <p style=""margin:0 0 24px;font-size:15px;line-height:1.6;"">{instruction}</p>
          <p style=""margin:0 0 24px;"">
            <a href=""{safeLink}"" style=""display:inline-block;background:#1d4ed8;color:#ffffff;text-decoration:none;padding:12px 28px;border-radius:8px;font-weight:600;font-size:15px;"">{buttonText}</a>
          </p>
          <p style=""margin:0 0 8px;font-size:13px;color:#6b7280;line-height:1.6;"">If the button does not work, copy and paste this link into your browser:</p>
          <p style=""margin:0 0 24px;font-size:12px;word-break:break-all;""><a href=""{safeLink}"" style=""color:#1d4ed8;"">{safeLink}</a></p>
          <p style=""margin:0;font-size:13px;color:#6b7280;line-height:1.6;"">{footnote}</p>
        </td></tr>
        <tr><td style=""padding:16px 32px;background:#f8fafc;border-top:1px solid #e3e8f0;font-size:12px;color:#9ca3af;"">
          This is an automated message from the {PortalName}. Please do not reply.
        </td></tr>
      </table>
    </td></tr>
  </table>
</body>
</html>";
        }
    }
}
