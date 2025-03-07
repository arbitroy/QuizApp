using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Pages.Quizzes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace QuizApp.Pages.Admin.Quizzes
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class CreateModel(ApplicationDbContext context) : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 3)]
            public string Title { get; set; } = string.Empty;

            public string? Description { get; set; }

            [Required]
            [Range(1, 120, ErrorMessage = "Time limit must be between 1 and 120 minutes.")]
            public int TimeLimit { get; set; }

            [Required]
            [MinLength(1, ErrorMessage = "At least one question is required.")]
            public List<QuestionInput> Questions { get; set; } = new List<QuestionInput>();
        }

        public class QuestionInput
        {
            [Required]
            public string Text { get; set; } = string.Empty;

            [Required]
            [MinLength(2, ErrorMessage = "At least two options are required.")]
            public List<OptionInput> Options { get; set; } = new List<OptionInput>();

            [Required]
            public int CorrectOptionIndex { get; set; }
        }

        public class OptionInput
        {
            [Required]
            public string Text { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            Input = new InputModel
            {
                TimeLimit = 10,
                Questions = new List<QuestionInput>
                {
                    new QuestionInput
                    {
                        Text = string.Empty,
                        Options = new List<OptionInput>
                        {
                            new OptionInput { Text = string.Empty },
                            new OptionInput { Text = string.Empty }
                        }
                    }
                }
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var quiz = new Quiz
            {
                Title = Input.Title,
                Description = Input.Description,
                TimeLimit = Input.TimeLimit,
                CreatedAt = DateTime.Now,
                Questions = new List<Question>()
            };

            foreach (var questionInput in Input.Questions)
            {
                var question = new Question
                {
                    Text = questionInput.Text,
                    Options = new List<Option>()
                };

                for (int i = 0; i < questionInput.Options.Count; i++)
                {
                    question.Options.Add(new Option
                    {
                        Text = questionInput.Options[i].Text,
                        IsCorrect = i == questionInput.CorrectOptionIndex
                    });
                }

                quiz.Questions.Add(question);
            }

            context.Quizzes?.Add(quiz);
            await context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}