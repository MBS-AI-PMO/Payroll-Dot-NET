using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PayslipMgtSystem.Models;
using PayslipMgtSystem.Services;

namespace PayslipMgtSystem.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AccountEmailSender _emailSender;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, AccountEmailSender emailSender, ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool EmailSent { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Please enter your email address.")]
            [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
            public string Email { get; set; } = "";
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var email = Input.Email.Trim();
            try
            {
                var user = await _userManager.FindByEmailAsync(email) ?? await _userManager.FindByNameAsync(email);
                if (user != null && user.EmailConfirmed && !string.IsNullOrEmpty(user.Email))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var baseUri = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/";
                    var link = AccountLinks.BuildResetPasswordLink(baseUri, user.Id, token, isSetup: user.PasswordHash == null);
                    await _emailSender.SendPasswordResetAsync(user.Email, user.Name, link);
                }
                else
                {
                    _logger.LogInformation("Password reset requested for unknown or disabled account {Email}", email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                ModelState.AddModelError(string.Empty, "We couldn't send the reset email right now. Please try again in a few minutes.");
                return Page();
            }

            // Same response whether or not the account exists, so emails can't be probed
            EmailSent = true;
            return Page();
        }
    }
}
