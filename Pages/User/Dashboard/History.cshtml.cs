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
using System.Threading.Tasks;

namespace QuizApp.Pages.User.Dashboard
{
    [Authorize]
    public class HistoryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HistoryModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
        public double AverageScore { get; set; }
        public double BestScore { get; set; }
        public double AverageDuration { get; set; }
        public HashSet<int> CompletedQuizIds { get; set; } = new HashSet<int>();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Get all completed quiz attempts
            QuizAttempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == user.Id && a.EndTime != null)
                .OrderByDescending(a => a.EndTime)
                .ToListAsync();

            // Calculate statistics
            if (QuizAttempts.Any())
            {
                AverageScore = QuizAttempts.Average(a => a.Score);
                BestScore = QuizAttempts.Max(a => a.Score);
                
                // Calculate average duration in minutes
                var durations = QuizAttempts
                    .Where(a => a.EndTime.HasValue)
                    .Select(a => (a.EndTime!.Value - a.StartTime).TotalMinutes);
                
                AverageDuration = durations.Any() ? durations.Average() : 0;
                
                // Get list of all quiz IDs that the user has completed
                CompletedQuizIds = QuizAttempts
                    .Select(a => a.QuizId)
                    .Distinct()
                    .ToHashSet();
            }

            return Page();
        }
    }
}