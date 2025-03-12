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
using System.Linq;
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

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            return Ok(statsDto);
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

            var historyDto = attempts.Select(a => new QuizAttemptSummaryDto
            {
                AttemptId = a.Id,
                QuizId = a.QuizId,
                QuizTitle = a.Quiz.Title,
                Score = a.Score,
                EndTime = a.EndTime.Value
            }).ToList();

            return Ok(historyDto);
        }
    }
}