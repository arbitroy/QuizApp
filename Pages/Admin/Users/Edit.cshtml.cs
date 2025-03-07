using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuizApp.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace QuizApp.Pages.Admin.Users
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class EditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        
        public EditModel(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            public string Id { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Username")]
            public string UserName { get; set; } = string.Empty;

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Display(Name = "Is Administrator")]
            public bool IsAdmin { get; set; }

            [Display(Name = "Set New Password")]
            public bool SetNewPassword { get; set; }

            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [Display(Name = "New Password")]
            public string? NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm Password")]
            [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
            public string? ConfirmPassword { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Don't allow admins to edit themselves through this page
            if (User.Identity != null && user.UserName == User.Identity.Name)
            {
                StatusMessage = "You cannot edit your own account through this page. Use the Identity management pages instead.";
                return RedirectToPage("./Index");
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");

            Input = new InputModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                IsAdmin = isAdmin
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.FindByIdAsync(Input.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Update basic user info
            if (user.Email != Input.Email)
            {
                user.Email = Input.Email;
                user.UserName = Input.Email; // Use email as username
                user.EmailConfirmed = true;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Update user role
            var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
            
            if (Input.IsAdmin && !isAdmin)
            {
                // Add to admin role
                await _userManager.RemoveFromRoleAsync(user, "User");
                await _userManager.AddToRoleAsync(user, "Administrator");
            }
            else if (!Input.IsAdmin && isAdmin)
            {
                // Remove from admin role
                await _userManager.RemoveFromRoleAsync(user, "Administrator");
                await _userManager.AddToRoleAsync(user, "User");
            }

            // Update password if requested
            if (Input.SetNewPassword && !string.IsNullOrEmpty(Input.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, Input.NewPassword);
                
                if (!resetResult.Succeeded)
                {
                    foreach (var error in resetResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }
            }

            StatusMessage = "User updated successfully.";
            return RedirectToPage("./Index");
        }
    }
}