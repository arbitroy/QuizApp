using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using System.Threading.Tasks;

namespace QuizApp.Pages.Admin.Quizzes
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Quiz Quiz { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            Quiz = quiz;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .Include(q => q.Attempts)
                .ThenInclude(a => a.Answers)
                .FirstOrDefaultAsync(q => q.Id == Quiz.Id);

            if (quiz == null)
            {
                return NotFound();
            }

            // First, delete all quiz attempts and their answers
            foreach (var attempt in quiz.Attempts.ToList())
            {
                // Remove all user answers for this attempt
                foreach (var answer in attempt.Answers.ToList())
                {
                    _context.UserAnswers.Remove(answer);
                }
                
                // Remove the attempt itself
                _context.QuizAttempts.Remove(attempt);
            }
            
            // Save changes to remove attempts and answers first
            await _context.SaveChangesAsync();

            // Now we can safely remove the quiz
            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}