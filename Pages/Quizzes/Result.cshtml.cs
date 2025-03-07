using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using System.Linq;
using System.Threading.Tasks;

namespace QuizApp.Pages.Quizzes
{
    [Authorize]
    public class ResultModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : PageModel
    {
        public QuizAttempt Attempt { get; set; } = null!;
        public Quiz Quiz { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var attempt = await context.QuizAttempts
                .Include(a => a.Quiz)
                .Include(a => a.Answers)
                .ThenInclude(a => a.Question)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (attempt == null || attempt.UserId != user.Id)
            {
                return NotFound();
            }

            Attempt = attempt;
            Quiz = attempt.Quiz ?? throw new InvalidOperationException("Quiz not found for this attempt");

            return Page();
        }
    }
}