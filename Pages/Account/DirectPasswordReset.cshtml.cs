using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuizApp.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace QuizApp.Pages.Account
{
    public class DirectPasswordResetModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DirectPasswordResetModel> _logger;

        public DirectPasswordResetModel(
            UserManager<ApplicationUser> userManager,
            ILogger<DirectPasswordResetModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet(string? email = null)
        {
            if (!string.IsNullOrEmpty(email))
            {
                Input = new InputModel { Email = email };
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                ModelState.AddModelError(string.Empty, "User not found or invalid email address.");
                return Page();
            }

            try
            {
                // Generate a reset token and immediately use it
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, resetToken, Input.NewPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"Password directly reset for user: {Input.Email}");
                    StatusMessage = "Your password has been reset successfully. You can now log in with your new password.";
                    return RedirectToPage("./Login", new { resetSuccess = true });
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during direct password reset");
                ModelState.AddModelError(string.Empty, "An error occurred during password reset. Please try again.");
                return Page();
            }
        }
    }
}