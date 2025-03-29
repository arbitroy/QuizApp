using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Api.Models;
using QuizApp.Data;
using QuizApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuizApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserController> _logger;

        public UserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<UserController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/user/profile
        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);

            var attempts = await _context.QuizAttempts
                .Where(a => a.UserId == user.Id && a.EndTime != null)
                .ToListAsync();

            var profileDto = new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                LastLoginTime = user.LastLoginTime,
                Roles = roles.ToList(),
                QuizzesTaken = attempts.Count,
                AverageScore = attempts.Any() ? attempts.Average(a => a.Score) : 0
            };

            return Ok(profileDto);
        }

        // PUT: api/user/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Check if username should be updated independently
            if (!string.IsNullOrEmpty(model.UserName) && model.UserName != user.UserName)
            {
                user.UserName = model.UserName; // Update username directly
            }

            // Check if email is changed and already exists
            if (!string.IsNullOrEmpty(model.Email) && model.Email != user.Email)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    return BadRequest(new { Message = "Email already in use" });
                }

                user.Email = model.Email;
                // Keep this if you want email to update username, but make it conditional
                // user.UserName = model.Email; 
                user.EmailConfirmed = true;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            // Update password if requested
            if (model.UpdatePassword && !string.IsNullOrEmpty(model.NewPassword))
            {
                // Verify current password first
                var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
                if (!isPasswordValid)
                {
                    return BadRequest(new { Message = "Current password is incorrect" });
                }

                var changePasswordResult = await _userManager.ChangePasswordAsync(
                    user, model.CurrentPassword, model.NewPassword);

                if (!changePasswordResult.Succeeded)
                {
                    foreach (var error in changePasswordResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return BadRequest(ModelState);
                }
            }

            return Ok(new { Message = "Profile updated successfully" });
        }

        // GET: api/user/stats
        [HttpGet("stats")]
        public async Task<ActionResult<UserStatsDto>> GetStats()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var attempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == user.Id && a.EndTime != null)
                .OrderByDescending(a => a.EndTime)
                .ToListAsync();

            var statsDto = new UserStatsDto
            {
                TotalQuizzesTaken = attempts.Count,
                AverageScore = attempts.Any() ? attempts.Average(a => a.Score) : 0,
                BestScore = attempts.Any() ? attempts.Max(a => a.Score) : 0,
                RecentAttempts = attempts.Take(5).Select(a => new QuizAttemptSummaryDto
                {
                    AttemptId = a.Id,
                    QuizId = a.QuizId,
                    QuizTitle = a.Quiz.Title,
                    Score = a.Score,
                    EndTime = a.EndTime.Value
                }).ToList()
            };

            // Get category performance - in a real app, you'd have categories defined
            var categoryPerformance = attempts
                .GroupBy(a => GetCategoryFromQuizTitle(a.Quiz.Title))
                .Select(g => new CategoryStatsDto
                {
                    Category = g.Key,
                    Count = g.Count(),
                    AverageScore = g.Average(a => a.Score)
                })
                .OrderByDescending(c => c.Count)
                .ToList();

            statsDto.CategoryPerformance = categoryPerformance;

            return Ok(statsDto);
        }

        // Helper method to get a category from quiz title - in a real app, you'd have proper categories
        private string GetCategoryFromQuizTitle(string quizTitle)
        {
            // Extract a potential category from the quiz title
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

        // GET: api/user/history
        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<QuizAttemptSummaryDto>>> GetHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var attempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == user.Id && a.EndTime != null)
                .OrderByDescending(a => a.EndTime)
                .ToListAsync();

            var historyDto = attempts.Select(a => new QuizAttemptDetailDto
            {
                AttemptId = a.Id,
                QuizId = a.QuizId,
                QuizTitle = a.Quiz.Title,
                Score = a.Score,
                StartTime = a.StartTime,
                EndTime = a.EndTime.Value,
                Duration = a.EndTime.HasValue ? (int)(a.EndTime.Value - a.StartTime).TotalMinutes : 0
            }).ToList();

            return Ok(historyDto);
        }

        // GET: api/user/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<UserDashboardDto>> GetDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Get recent attempts
            var recentAttempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == user.Id && a.EndTime != null)
                .OrderByDescending(a => a.EndTime)
                .Take(5)
                .ToListAsync();

            // Get user stats
            var allAttempts = await _context.QuizAttempts
                .Where(a => a.UserId == user.Id && a.EndTime != null)
                .ToListAsync();

            // Get recommended quizzes (ones the user hasn't taken or hasn't done well on)
            var takenQuizIds = allAttempts
                .Where(a => a.Score >= 80) // Exclude quizzes where user scored well
                .Select(a => a.QuizId)
                .Distinct()
                .ToList();

            var recommendedQuizzes = await _context.Quizzes
                .Include(q => q.Questions)
                .Where(q => !takenQuizIds.Contains(q.Id))
                .OrderBy(q => Guid.NewGuid()) // Random order
                .Take(3)
                .Select(q => new QuizDto
                {
                    Id = q.Id,
                    Title = q.Title,
                    Description = q.Description,
                    TimeLimit = q.TimeLimit,
                    CreatedAt = q.CreatedAt,
                    QuestionCount = q.Questions.Count
                })
                .ToListAsync();

            var dashboard = new UserDashboardDto
            {
                TotalQuizzesTaken = allAttempts.Count,
                AverageScore = allAttempts.Any() ? allAttempts.Average(a => a.Score) : 0,
                BestScore = allAttempts.Any() ? allAttempts.Max(a => a.Score) : 0,
                RecentAttempts = recentAttempts.Select(a => new QuizAttemptSummaryDto
                {
                    AttemptId = a.Id,
                    QuizId = a.QuizId,
                    QuizTitle = a.Quiz.Title,
                    Score = a.Score,
                    EndTime = a.EndTime.Value
                }).ToList(),
                RecommendedQuizzes = recommendedQuizzes
            };

            return Ok(dashboard);
        }
    }

    // DTOs for User API
    public class UpdateProfileDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string UserName { get; set; }  // Add this property

        public bool UpdatePassword { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 8)]
        public string? CurrentPassword { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 8)]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string? ConfirmPassword { get; set; }
    }

    public class QuizAttemptDetailDto : QuizAttemptSummaryDto
    {
        public DateTime StartTime { get; set; }
        public int Duration { get; set; } // in minutes
    }

    public class CategoryStatsDto
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public double AverageScore { get; set; }
    }

    public class UserDashboardDto
    {
        public int TotalQuizzesTaken { get; set; }
        public double AverageScore { get; set; }
        public double BestScore { get; set; }
        public List<QuizAttemptSummaryDto> RecentAttempts { get; set; } = new List<QuizAttemptSummaryDto>();
        public List<QuizDto> RecommendedQuizzes { get; set; } = new List<QuizDto>();
    }
}