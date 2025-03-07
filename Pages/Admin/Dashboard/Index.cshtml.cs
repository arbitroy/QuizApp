using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Pages.Quizzes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace  QuizApp.Pages.Admin.Dashboard
{
    [Authorize]
    public class IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : PageModel
    {
        public ApplicationUser CurrentUser { get; set; } = null!;
        public IList<QuizAttempt> RecentAttempts { get; set; } = new List<QuizAttempt>();
        public int TotalQuizzesTaken { get; set; }
        public double AverageScore { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
            {
                return NotFound();
            }

            CurrentUser = user;

            // Get the 5 most recent quiz attempts
            RecentAttempts = await context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == CurrentUser.Id && a.EndTime != null)
                .OrderByDescending(a => a.EndTime)
                .Take(5)
                .ToListAsync();

            // Calculate stats
            TotalQuizzesTaken = await context.QuizAttempts
                .Where(a => a.UserId == CurrentUser.Id && a.EndTime != null)
                .CountAsync();

            if (TotalQuizzesTaken > 0)
            {
                AverageScore = await context.QuizAttempts
                    .Where(a => a.UserId == CurrentUser.Id && a.EndTime != null)
                    .AverageAsync(a => a.Score);
            }

            return Page();
        }
    }
}