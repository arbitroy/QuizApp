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
    public class QuizzesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public QuizzesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/quizzes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QuizDto>>> GetQuizzes()
        {
            var quizzes = await _context.Quizzes
                .Include(q => q.Questions)
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

            return Ok(quizzes);
        }

        // GET: api/quizzes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<QuizDetailDto>> GetQuiz(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            var quizDto = new QuizDetailDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                TimeLimit = quiz.TimeLimit,
                CreatedAt = quiz.CreatedAt,
                QuestionCount = quiz.Questions.Count,
                Questions = quiz.Questions.Select(q => new QuestionDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    Options = q.Options.Select(o => new OptionDto
                    {
                        Id = o.Id,
                        Text = o.Text
                        // Note: We don't include the IsCorrect property for security reasons
                    }).ToList()
                }).ToList()
            };

            return Ok(quizDto);
        }

        // POST: api/quizzes/take
        [HttpPost("take")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<QuizResultDto>> TakeQuiz(QuizSubmissionDto submission)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            // Get the quiz
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == submission.QuizId);

            if (quiz == null)
            {
                return NotFound("Quiz not found");
            }

            // Create a new attempt
            var attempt = new QuizAttempt
            {
                QuizId = quiz.Id,
                UserId = userId,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };

            _context.QuizAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            // Calculate score and record answers
            int correctAnswers = 0;

            foreach (var answer in submission.Answers)
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

                    _context.UserAnswers.Add(userAnswer);

                    // Check if the answer is correct
                    var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                    if (correctOption != null && correctOption.Id == selectedOptionId)
                    {
                        correctAnswers++;
                    }
                }
            }

            // Update the attempt with the score
            attempt.Score = (int)Math.Round((double)correctAnswers / quiz.Questions.Count * 100);
            _context.QuizAttempts.Update(attempt);
            await _context.SaveChangesAsync();

            // Return the result
            return await GetQuizResult(attempt.Id);
        }

        // GET: api/quizzes/result/5
        [HttpGet("result/{attemptId}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<QuizResultDto>> GetQuizResult(int attemptId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var attempt = await _context.QuizAttempts
                .Include(a => a.Quiz)
                .Include(a => a.Answers)
                .ThenInclude(a => a.Question)
                .ThenInclude(q => q.Options)
                .Include(a => a.Answers)
                .ThenInclude(a => a.SelectedOption)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt == null)
            {
                return NotFound("Attempt not found");
            }

            // Check if the attempt belongs to the current user, unless they're an admin
            if (attempt.UserId != userId && !User.IsInRole("Administrator"))
            {
                return Forbid("You don't have permission to view this attempt");
            }

            var resultDto = new QuizResultDto
            {
                AttemptId = attempt.Id,
                QuizId = attempt.QuizId,
                QuizTitle = attempt.Quiz.Title,
                Score = attempt.Score,
                StartTime = attempt.StartTime,
                EndTime = attempt.EndTime,
                Questions = attempt.Answers.Select(a =>
                {
                    var correctOption = a.Question.Options.FirstOrDefault(o => o.IsCorrect);
                    return new QuestionResultDto
                    {
                        QuestionId = a.QuestionId,
                        QuestionText = a.Question.Text,
                        SelectedOptionId = a.SelectedOptionId,
                        SelectedOptionText = a.SelectedOption?.Text ?? "No answer",
                        CorrectOptionId = correctOption?.Id ?? 0,
                        CorrectOptionText = correctOption?.Text ?? "Unknown",
                        IsCorrect = a.SelectedOption?.IsCorrect ?? false
                    };
                }).ToList()
            };

            return Ok(resultDto);
        }
    }
}