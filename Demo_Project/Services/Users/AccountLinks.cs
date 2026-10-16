using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace PayslipMgtSystem.Services.Users
{
    /// <summary>CNIC normalisation and the signed links used for password setup/reset emails.</summary>
    public static class AccountLinks
    {
        public static string NormalizeCnic(string? cnic) =>
            new string((cnic ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        public static string EncodeToken(string token) =>
            WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        public static string DecodeToken(string code) =>
            Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));

        public static string BuildResetPasswordLink(string baseUri, string userId, string token, bool isSetup)
        {
            var url = baseUri.TrimEnd('/') + "/Identity/Account/ResetPassword";
            return QueryHelpers.AddQueryString(url, new Dictionary<string, string?>
            {
                ["userId"] = userId,
                ["code"] = EncodeToken(token),
                ["mode"] = isSetup ? "setup" : "reset"
            });
        }
    }
}
