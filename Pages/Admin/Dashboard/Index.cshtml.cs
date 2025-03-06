﻿using Microsoft.AspNetCore.Authorization;
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

namespace QuizApp.Pages.Dashboard
{
    [Authorize]
    public class IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : PageModel
    {
        public ApplicationUser? CurrentUser { get; set; }
        public IList<QuizAttempt>? RecentAttempts { get; set; }
        public int TotalQuizzesTaken { get; set; }
        public double AverageScore { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            CurrentUser = await userManager.GetUserAsync(User);

            if (CurrentUser == null)
            {
                return NotFound();
            }

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