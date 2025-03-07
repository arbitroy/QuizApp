using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizApp.Models;
using QuizApp.Pages.Quizzes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuizApp.Pages.Admin.Users
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class IndexModel(UserManager<ApplicationUser> userManager) : PageModel
    {
        public IList<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            Users = await userManager.Users.ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var user = await userManager.FindByIdAsync(id);
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

            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                StatusMessage = "User deleted successfully.";
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            Users = await userManager.Users.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(string id)
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Generate password reset token
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            // Set temporary password
            var tempPassword = "Temp123!";
            var result = await userManager.ResetPasswordAsync(user, token, tempPassword);

            if (result.Succeeded)
            {
                StatusMessage = $"Password for {user.Email} reset to: {tempPassword}";
                return RedirectToPage();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            Users = await userManager.Users.ToListAsync();
            return Page();
        }
    }
}