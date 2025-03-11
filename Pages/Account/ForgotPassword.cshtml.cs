using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using QuizApp.Models;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace QuizApp.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [TempData]
        public string ResetLink { get; set; } = string.Empty;

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                _logger.LogInformation($"Processing forgot password request for email: {Input.Email}");
                
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    _logger.LogWarning($"User not found for email: {Input.Email}");
                    StatusMessage = "If that email is registered, we've sent a password reset link.";
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                // Generate the reset token
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                _logger.LogInformation($"Generated password reset token for user: {Input.Email}");
                
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { code },
                    protocol: Request.Scheme);

                _logger.LogInformation($"Generated reset URL: {callbackUrl}");

                // Store the reset link instead of sending via email
                ResetLink = callbackUrl;
                
                return RedirectToPage("./ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error in forgot password process: {ex.Message}");
                StatusMessage = "An unexpected error occurred. Please try again.";
                return Page();
            }
        }
    }
}