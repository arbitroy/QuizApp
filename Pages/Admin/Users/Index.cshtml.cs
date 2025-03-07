using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizApp.Pages.Admin.Users
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        
        public IndexModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public IList<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

        [TempData]
        public string? StatusMessage { get; set; }

        [BindProperty]
        public List<string> SelectedUsers { get; set; } = new List<string>();

        public async Task OnGetAsync()
        {
            Users = await _userManager.Users.ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Don't allow admins to delete themselves
            if (User.Identity != null && user.UserName == User.Identity.Name)
            {
                StatusMessage = "Error: You cannot delete your own account.";
                return RedirectToPage();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = "User deleted successfully.";
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            Users = await _userManager.Users.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Set temporary password
            var tempPassword = "Temp123!";
            var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);

            if (result.Succeeded)
            {
                StatusMessage = $"Password for {user.Email} reset to: {tempPassword}";
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            Users = await _userManager.Users.ToListAsync();
            return Page();
        }
        
        public async Task<IActionResult> OnPostBulkDeleteAsync()
        {
            if (SelectedUsers == null || !SelectedUsers.Any())
            {
                StatusMessage = "Error: No users selected for deletion.";
                return RedirectToPage();
            }

            var currentUserName = User.Identity?.Name;
            var successCount = 0;
            var errorCount = 0;
            var currentUserSelected = false;

            foreach (var userId in SelectedUsers)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    continue;
                }

                // Skip if trying to delete current user
                if (user.UserName == currentUserName)
                {
                    currentUserSelected = true;
                    continue;
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    successCount++;
                }
                else
                {
                    errorCount++;
                }
            }

            if (successCount > 0)
            {
                StatusMessage = $"Successfully deleted {successCount} user(s).";
                if (errorCount > 0)
                {
                    StatusMessage += $" Failed to delete {errorCount} user(s).";
                }
                if (currentUserSelected)
                {
                    StatusMessage += " Your own account was not deleted.";
                }
            }
            else if (currentUserSelected && errorCount == 0)
            {
                StatusMessage = "Error: You cannot delete your own account.";
            }
            else
            {
                StatusMessage = $"Error: Failed to delete {errorCount} user(s).";
            }

            return RedirectToPage();
        }
    }
}