using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace QuizApp.Pages.Admin.Quizzes
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = default!;

        public class InputModel
        {
            public int Id { get; set; }

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
            public int Id { get; set; }

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
            public int Id { get; set; }

            [Required]
            public string Text { get; set; } = string.Empty;

            public bool IsCorrect { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            // Map the data from the database to our input model
            Input = new InputModel
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description ?? string.Empty,
                TimeLimit = quiz.TimeLimit,
                Questions = quiz.Questions.Select(q => new QuestionInput
                {
                    Id = q.Id,
                    Text = q.Text,
                    Options = q.Options.Select((o, index) => new OptionInput
                    {
                        Id = o.Id,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect
                    }).ToList(),
                    CorrectOptionIndex = q.Options.ToList().FindIndex(o => o.IsCorrect)
                }).ToList()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Get the existing quiz from the database
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == Input.Id);

            if (quiz == null)
            {
                return NotFound();
            }

            // Update basic quiz properties
            quiz.Title = Input.Title;
            quiz.Description = Input.Description;
            quiz.TimeLimit = Input.TimeLimit;

            // Track existing question IDs to find questions to delete
            var existingQuestionIds = quiz.Questions.Select(q => q.Id).ToList();
            var updatedQuestionIds = Input.Questions.Where(q => q.Id > 0).Select(q => q.Id).ToList();
            var questionsToDelete = existingQuestionIds.Except(updatedQuestionIds).ToList();

            // Delete questions that are no longer in the updated quiz
            foreach (var questionId in questionsToDelete)
            {
                var questionToDelete = quiz.Questions.FirstOrDefault(q => q.Id == questionId);
                if (questionToDelete != null)
                {
                    _context.Questions.Remove(questionToDelete);
                }
            }

            // Update existing questions and add new ones
            foreach (var inputQuestion in Input.Questions)
            {
                if (inputQuestion.Id > 0)
                {
                    // Update existing question
                    var existingQuestion = quiz.Questions.FirstOrDefault(q => q.Id == inputQuestion.Id);
                    if (existingQuestion != null)
                    {
                        existingQuestion.Text = inputQuestion.Text;

                        // Track existing option IDs to find options to delete
                        var existingOptionIds = existingQuestion.Options.Select(o => o.Id).ToList();
                        var updatedOptionIds = inputQuestion.Options.Where(o => o.Id > 0).Select(o => o.Id).ToList();
                        var optionsToDelete = existingOptionIds.Except(updatedOptionIds).ToList();

                        // Delete options that are no longer in the updated question
                        foreach (var optionId in optionsToDelete)
                        {
                            var optionToDelete = existingQuestion.Options.FirstOrDefault(o => o.Id == optionId);
                            if (optionToDelete != null)
                            {
                                _context.Options.Remove(optionToDelete);
                            }
                        }

                        // Update existing options and add new ones
                        for (int i = 0; i < inputQuestion.Options.Count; i++)
                        {
                            var inputOption = inputQuestion.Options[i];
                            var isCorrect = i == inputQuestion.CorrectOptionIndex;

                            if (inputOption.Id > 0)
                            {
                                // Update existing option
                                var existingOption = existingQuestion.Options.FirstOrDefault(o => o.Id == inputOption.Id);
                                if (existingOption != null)
                                {
                                    existingOption.Text = inputOption.Text;
                                    existingOption.IsCorrect = isCorrect;
                                }
                            }
                            else
                            {
                                // Add new option
                                existingQuestion.Options.Add(new Option
                                {
                                    Text = inputOption.Text,
                                    IsCorrect = isCorrect,
                                    QuestionId = existingQuestion.Id
                                });
                            }
                        }
                    }
                }
                else
                {
                    // Add new question
                    var newQuestion = new Question
                    {
                        Text = inputQuestion.Text,
                        QuizId = quiz.Id,
                        Options = new List<Option>()
                    };

                    for (int i = 0; i < inputQuestion.Options.Count; i++)
                    {
                        newQuestion.Options.Add(new Option
                        {
                            Text = inputQuestion.Options[i].Text,
                            IsCorrect = i == inputQuestion.CorrectOptionIndex
                        });
                    }

                    quiz.Questions.Add(newQuestion);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuizExists(Input.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool QuizExists(int id)
        {
            return _context.Quizzes.Any(e => e.Id == id);
        }
    }
}