using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
    public class IndexModel : SecurePageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public ApplicationUser CurrentUser { get; set; } = null!;
        public IList<QuizAttempt> RecentAttempts { get; set; } = new List<QuizAttempt>();
        public IList<Quiz> RecommendedQuizzes { get; set; } = new List<Quiz>();
        public int TotalQuizzesTaken { get; set; }
        public double AverageScore { get; set; }
        public double BestScore { get; set; }
        public QuizAttempt? LastAttempt { get; set; }
        public List<CategoryStats> CategoryPerformance { get; set; } = new List<CategoryStats>();

        // Renamed this class to avoid naming conflict with the property
        public class CategoryStats
        {
            public string? Category { get; set; }
            public int Count { get; set; }
            public double AverageScore { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // This check is redundant with our SecurePageModel, but we'll keep it for extra safety
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            CurrentUser = user;

            // Get recent quiz attempts (limited to 5)
            RecentAttempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == CurrentUser.Id && a.EndTime != null)
                .OrderByDescending(a => a.EndTime)
                .Take(5)
                .ToListAsync();

            // Last attempt for quick actions
            LastAttempt = RecentAttempts.FirstOrDefault();

            // Calculate quiz statistics
            var allAttempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == CurrentUser.Id && a.EndTime != null)
                .ToListAsync();

            TotalQuizzesTaken = allAttempts.Count;

            if (TotalQuizzesTaken > 0)
            {
                AverageScore = allAttempts.Average(a => a.Score);
                BestScore = allAttempts.Max(a => a.Score);
                
                // Create mock category performance data - in a real app you would have categories defined
                // This is a simulation based on quiz titles for demonstration
                CategoryPerformance = allAttempts
                    .GroupBy(a => GetCategoryFromQuizTitle(a.Quiz.Title))
                    .Select(g => new CategoryStats
                    {
                        Category = g.Key,
                        Count = g.Count(),
                        AverageScore = g.Average(a => a.Score)
                    })
                    .OrderByDescending(c => c.Count)
                    .ToList();
            }

            // Get recommended quizzes - ones the user hasn't taken yet or hasn't done well on
            var takenQuizIds = allAttempts
                .Where(a => a.Score >= 80) // Exclude quizzes where the user scored well
                .Select(a => a.QuizId)
                .Distinct()
                .ToList();

            RecommendedQuizzes = await _context.Quizzes
                .Include(q => q.Questions)
                .Where(q => !takenQuizIds.Contains(q.Id)) // Only quizzes not taken or not aced
                .OrderBy(q => Guid.NewGuid()) // Random order
                .Take(3)
                .ToListAsync();

            return Page();
        }

        // Helper method to get a category from quiz title - in a real app, you'd have proper categories
        private string GetCategoryFromQuizTitle(string quizTitle)
        {
            // Extract a potential category from the quiz title
            // This is just for demonstration - in a real app, quizzes would have explicit categories
            if (quizTitle.Contains("Science", StringComparison.OrdinalIgnoreCase))
                return "Science";
            else if (quizTitle.Contains("Math", StringComparison.OrdinalIgnoreCase) || 
                     quizTitle.Contains("Mathematics", StringComparison.OrdinalIgnoreCase))
                return "Mathematics";
            else if (quizTitle.Contains("History", StringComparison.OrdinalIgnoreCase))
                return "History";
            else if (quizTitle.Contains("Geography", StringComparison.OrdinalIgnoreCase))
                return "Geography";
            else if (quizTitle.Contains("Literature", StringComparison.OrdinalIgnoreCase) || 
                     quizTitle.Contains("English", StringComparison.OrdinalIgnoreCase))
                return "Literature";
            else if (quizTitle.Contains("Programming", StringComparison.OrdinalIgnoreCase) || 
                     quizTitle.Contains("Coding", StringComparison.OrdinalIgnoreCase) ||
                     quizTitle.Contains("Computer", StringComparison.OrdinalIgnoreCase))
                return "Computer Science";
            
            return "General Knowledge";
        }
    }
}