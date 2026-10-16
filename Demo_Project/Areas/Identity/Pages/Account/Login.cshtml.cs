using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace PayslipMgtSystem.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public class LoginModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UserCacheService userCacheService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager,
            ILogger<LoginModel> logger,
            UserManager<ApplicationUser> userManager,
            UserCacheService userCacheService)
        {
            _userManager = userManager;
            this.userCacheService = userCacheService;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Please enter your email address.")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Please enter your password.")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null, bool signedOut = false)
        {
            if (_signInManager.IsSignedIn(User))
            {
                await _signInManager.SignOutAsync();
            }
            if (signedOut && string.IsNullOrEmpty(StatusMessage))
            {
                StatusMessage = "You have been signed out successfully.";
            }
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            if (ModelState.IsValid)
            {
                var userTemp = await _userManager.FindByNameAsync(Input.Email.ToLower());
                if (userTemp == null)
                {
                    ModelState.AddModelError(string.Empty, "The email or password you entered is incorrect.");
                    return Page();
                }
                var emailConf = await _userManager.IsEmailConfirmedAsync(userTemp);
                if (!emailConf)
                {
                    ModelState.AddModelError(string.Empty, "Your account is disabled. Please contact your HR department.");
                    return Page();
                }
                if (userTemp.PasswordHash == null)
                {
                    ModelState.AddModelError(string.Empty, "You have not set a password yet. Use the link in your welcome email, or choose \"Forgot password?\" to receive a new one.");
                    return Page();
                }
                var result = await _signInManager.PasswordSignInAsync(Input.Email.ToLower(), Input.Password, Input.RememberMe, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();

                    await userCacheService.SetSessionTime(Input.Email, DateTime.UtcNow.AddHours(5));
                    await userCacheService.SetUserIp(Input.Email, remoteIpAddress);

                    var isEmployeeOnly = await _userManager.IsInRoleAsync(userTemp, AppRoles.Employee)
                                         && !await _userManager.IsInRoleAsync(userTemp, AppRoles.Admin);
                    if (isEmployeeOnly)
                    {
                        var target = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                                 && (returnUrl.StartsWith("/payslips", StringComparison.OrdinalIgnoreCase) || returnUrl.StartsWith("/tax-certificates", StringComparison.OrdinalIgnoreCase))
                            ? returnUrl : "/payslips";
                        return LocalRedirect(target);
                    }
                    return LocalRedirect(!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    ModelState.AddModelError(string.Empty, "Your account is temporarily locked after several failed sign-in attempts. Please try again in a few minutes or reset your password.");
                    return Page();
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "The email or password you entered is incorrect.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
