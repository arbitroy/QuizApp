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

namespace QuizApp.Pages.Quizzes
{
    [Authorize]
    public class TakeModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager) : PageModel
    {
        public Quiz? Quiz { get; set; }
        public QuizAttempt? Attempt { get; set; }

        [BindProperty]
        public Dictionary<int, int> Answers { get; set; } = new Dictionary<int, int>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Quiz = await context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (Quiz == null)
            {
                return NotFound();
            }

            var user = await userManager.GetUserAsync(User);

            // Create a new attempt
            Attempt = new QuizAttempt
            {
                QuizId = Quiz.Id,
                UserId = user.Id,
                StartTime = DateTime.Now
            };

            context.QuizAttempts.Add(Attempt);
            await context.SaveChangesAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var user = await userManager.GetUserAsync(User);

            var quiz = await context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            // Find the active attempt
            var attempt = await context.QuizAttempts
                .FirstOrDefaultAsync(a => a.QuizId == id && a.UserId == user.Id && a.EndTime == null);

            if (attempt == null)
            {
                return NotFound();
            }

            // Calculate score and record answers
            int correctAnswers = 0;

            foreach (var answer in Answers)
            {
                var questionId = answer.Key;
                var selectedOptionId = answer.Value;

                var question = quiz.Questions.FirstOrDefault(q => q.Id == questionId);
                if (question != null)
                {
                    var userAnswer = new UserAnswer
                    {
                        QuizAttemptId = attempt.Id,
                        QuestionId = questionId,
                        SelectedOptionId = selectedOptionId
                    };

                    context.UserAnswers.Add(userAnswer);

                    // Check if the answer is correct
                    var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                    if (correctOption != null && correctOption.Id == selectedOptionId)
                    {
                        correctAnswers++;
                    }
                }
            }

            // Update the attempt
            attempt.EndTime = DateTime.Now;
            attempt.Score = (int)Math.Round((double)correctAnswers / quiz.Questions.Count() * 100);

            await context.SaveChangesAsync();

            return RedirectToPage("./Result", new { id = attempt.Id });
        }
    }
}