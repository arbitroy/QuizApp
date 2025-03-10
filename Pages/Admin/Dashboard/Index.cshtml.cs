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

namespace QuizApp.Pages.Admin.Dashboard
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public IList<Quiz> Quizzes { get; set; } = new List<Quiz>();
        public IList<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
        public IList<QuizAttempt> RecentAttempts { get; set; } = new List<QuizAttempt>();
        public double AverageScore { get; set; }
        public IList<UserWithRole> NewUsers { get; set; } = new List<UserWithRole>();
        public IList<QuizPopularity> PopularQuizzes { get; set; } = new List<QuizPopularity>();

        public class UserWithRole
        {
            public string Id { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public DateTime LastLoginTime { get; set; }
            public bool IsAdmin { get; set; }
        }

        public class QuizPopularity
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public int AttemptCount { get; set; }
            public double AverageScore { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get all users
            Users = await _userManager.Users.ToListAsync();

            // Get all quizzes
            Quizzes = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Attempts)
                .ToListAsync();

            // Get all quiz attempts
            QuizAttempts = await _context.QuizAttempts
                .Include(a => a.User)
                .Include(a => a.Quiz)
                .Where(a => a.EndTime != null)
                .ToListAsync();

            // Calculate average score
            if (QuizAttempts.Any())
            {
                AverageScore = QuizAttempts.Average(a => a.Score);
            }

            // Get recent quiz attempts (limited to 10)
            RecentAttempts = await _context.QuizAttempts
                .Include(a => a.User)
                .Include(a => a.Quiz)
                .Where(a => a.EndTime != null)
                .OrderByDescending(a => a.EndTime)
                .Take(10)
                .ToListAsync();

            // Get newest users (limited to 5)
            var newestUsers = await _userManager.Users
                .OrderByDescending(u => u.LastLoginTime)
                .Take(5)
                .ToListAsync();

            NewUsers = new List<UserWithRole>();
            foreach (var user in newestUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                NewUsers.Add(new UserWithRole
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "Unknown",
                    LastLoginTime = user.LastLoginTime,
                    IsAdmin = roles.Contains("Administrator")
                });
            }

            // Get popular quizzes based on attempt count
            PopularQuizzes = Quizzes
                .Where(q => q.Attempts.Any())
                .Select(q => new QuizPopularity
                {
                    Id = q.Id,
                    Title = q.Title,
                    AttemptCount = q.Attempts.Count,
                    AverageScore = q.Attempts.Any() ? q.Attempts.Average(a => a.Score) : 0
                })
                .OrderByDescending(q => q.AttemptCount)
                .Take(5)
                .ToList();

            return Page();
        }
    }
}