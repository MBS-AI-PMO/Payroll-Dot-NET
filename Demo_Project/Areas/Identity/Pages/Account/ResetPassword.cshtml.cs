using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using PayslipMgtSystem.Models;
using PayslipMgtSystem.Services;

namespace PayslipMgtSystem.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager, IOptions<IdentityOptions> identityOptions, ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
            PasswordRules = identityOptions.Value.Password;
        }

        public PasswordOptions PasswordRules { get; }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool LinkInvalid { get; set; }
        public bool IsSetup => string.Equals(Input.Mode, "setup", StringComparison.OrdinalIgnoreCase);
        public string? AccountEmail { get; set; }

        public class InputModel
        {
            public string UserId { get; set; } = "";
            public string Code { get; set; } = "";
            public string? Mode { get; set; }

            [Required(ErrorMessage = "Please enter a new password.")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [Required(ErrorMessage = "Please confirm your new password.")]
            [DataType(DataType.Password)]
            [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
            public string ConfirmPassword { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync(string? userId = null, string? code = null, string? mode = null)
        {
            Input = new InputModel { UserId = userId ?? "", Code = code ?? "", Mode = mode };
            var user = await ValidateLinkAsync();
            if (user == null)
                LinkInvalid = true;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Password fields are validated below; the hidden link fields are validated here
            var user = await ValidateLinkAsync();
            if (user == null)
            {
                LinkInvalid = true;
                return Page();
            }

            if (!ModelState.IsValid)
                return Page();

            var result = await _userManager.ResetPasswordAsync(user, AccountLinks.DecodeToken(Input.Code), Input.Password);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
                {
                    LinkInvalid = true;
                    return Page();
                }
                foreach (var error in result.Errors)
                    ModelState.AddModelError("Input.Password", error.Description);
                return Page();
            }

            if (await _userManager.IsLockedOutAsync(user))
                await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            _logger.LogInformation("Password {Action} for user {UserId}", IsSetup ? "set" : "reset", user.Id);
            TempData["StatusMessage"] = IsSetup
                ? "Your password has been set. You can now sign in."
                : "Your password has been changed. Please sign in with your new password.";
            TempData["ToastType"] = "success";
            TempData["ToastMessage"] = IsSetup ? "Password set successfully." : "Password changed successfully.";
            return RedirectToPage("./Login");
        }

        private async Task<ApplicationUser?> ValidateLinkAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.UserId) || string.IsNullOrWhiteSpace(Input.Code))
                return null;

            try
            {
                var user = await _userManager.FindByIdAsync(Input.UserId);
                if (user == null)
                    return null;

                var token = AccountLinks.DecodeToken(Input.Code);
                var valid = await _userManager.VerifyUserTokenAsync(user,
                    _userManager.Options.Tokens.PasswordResetTokenProvider,
                    UserManager<ApplicationUser>.ResetPasswordTokenPurpose, token);
                if (!valid)
                    return null;

                AccountEmail = user.Email;
                return user;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
