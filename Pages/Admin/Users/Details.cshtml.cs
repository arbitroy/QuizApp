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

namespace QuizApp.Pages.Admin.Users
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class DetailsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        
        public DetailsModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public ApplicationUser UserDetails { get; set; } = default!;
        public List<string> UserRoles { get; set; } = new List<string>();
        public int QuizzesTaken { get; set; }
        public double AverageScore { get; set; }
        public DateTime? LastQuizAttempt { get; set; }
        public IList<QuizAttempt> RecentAttempts { get; set; } = new List<QuizAttempt>();

        public async Task<IActionResult> OnGetAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            UserDetails = user;
            
            // Get user roles
            UserRoles = (await _userManager.GetRolesAsync(user)).ToList();

            // Get quiz statistics
            var attempts = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Where(a => a.UserId == user.Id && a.EndTime.HasValue)
                .ToListAsync();

            QuizzesTaken = attempts.Count;
            
            if (QuizzesTaken > 0)
            {
                AverageScore = attempts.Average(a => a.Score);
                LastQuizAttempt = attempts.Max(a => a.EndTime);
                
                // Get the 5 most recent attempts
                RecentAttempts = attempts
                    .OrderByDescending(a => a.EndTime)
                    .Take(5)
                    .ToList();
            }

            return Page();
        }
    }
}