using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizApp.Pages.Admin.Users
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        
        public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
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

        public async Task<IActionResult> OnGetDownloadReportAsync()
        {
            // Get all users with their quiz attempts
            var users = await _userManager.Users
                .Include(u => u.QuizAttempts)
                .ToListAsync();
            
            var userReportData = new List<UserReportData>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.Any() ? string.Join(", ", roles) : "No Role";
                
                // Get quiz statistics
                var completedQuizzes = user.QuizAttempts.Count(a => a.EndTime != null);
                var averageScore = user.QuizAttempts
                    .Where(a => a.EndTime != null)
                    .Select(a => a.Score)
                    .DefaultIfEmpty(0)
                    .Average();
                
                // Get last login timestamp
                var lastLoginFormatted = user.LastLoginTime.ToString("yyyy-MM-dd HH:mm:ss");
                
                userReportData.Add(new UserReportData
                {
                    Username = user.UserName ?? "N/A",
                    Email = user.Email ?? "N/A",
                    Role = roleName,
                    LastLoginTime = lastLoginFormatted,
                    QuizAttempts = completedQuizzes,
                    AverageScore = Math.Round(averageScore, 1),
                    IsEmailConfirmed = user.EmailConfirmed ? "Yes" : "No",
                    UserId = user.Id
                });
            }

            // Create CSV content
            var csv = new StringBuilder();
            csv.AppendLine("User ID,Username,Email,Role,Last Login,Quiz Attempts,Average Score (%),Email Confirmed");
            
            foreach (var userData in userReportData)
            {
                csv.AppendLine($"\"{userData.UserId}\",\"{userData.Username}\",\"{userData.Email}\",\"{userData.Role}\",\"{userData.LastLoginTime}\",{userData.QuizAttempts},{userData.AverageScore},\"{userData.IsEmailConfirmed}\"");
            }

            // Set the response headers for file download
            byte[] bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"user-report-{DateTime.Now:yyyy-MM-dd}.csv");
        }

        public class UserReportData
        {
            public string UserId { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string LastLoginTime { get; set; } = string.Empty;
            public int QuizAttempts { get; set; }
            public double AverageScore { get; set; }
            public string IsEmailConfirmed { get; set; } = string.Empty;
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