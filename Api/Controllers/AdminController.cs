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
    [Route("api/admin")]
    [ApiController]
    [Authorize(Policy = "ApiRequiresAdminRole")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: api/admin/users
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserProfileDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var attempts = await _context.QuizAttempts
                    .Where(a => a.UserId == user.Id && a.EndTime != null)
                    .ToListAsync();

                userDtos.Add(new UserProfileDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    LastLoginTime = user.LastLoginTime,
                    Roles = roles.ToList(),
                    QuizzesTaken = attempts.Count,
                    AverageScore = attempts.Any() ? attempts.Average(a => a.Score) : 0
                });
            }

            return Ok(userDtos);
        }

        // GET: api/admin/users/{id}
        [HttpGet("users/{id}")]
        public async Task<ActionResult<UserProfileDto>> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var attempts = await _context.QuizAttempts
                .Where(a => a.UserId == user.Id && a.EndTime != null)
                .ToListAsync();

            var userDto = new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                LastLoginTime = user.LastLoginTime,
                Roles = roles.ToList(),
                QuizzesTaken = attempts.Count,
                AverageScore = attempts.Any() ? attempts.Average(a => a.Score) : 0
            };

            return Ok(userDto);
        }

        // PUT: api/admin/users/{id}
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto userDto)
        {
            if (id != userDto.Id)
            {
                return BadRequest("User ID mismatch");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            if (!string.IsNullOrEmpty(userDto.UserName) && userDto.UserName != user.UserName)
            {
                user.UserName = userDto.UserName; // Update username directly
            }

            if (!string.IsNullOrEmpty(userDto.Email) && userDto.Email != user.Email)
            {
                // Check if email is already taken
                var existingUser = await _userManager.FindByEmailAsync(userDto.Email);
                if (existingUser != null && existingUser.Id != id)
                {
                    return BadRequest("Email is already taken");
                }

                user.Email = userDto.Email;
                // Remove this line to allow independent username:
                // user.UserName = userDto.Email; 
                user.EmailConfirmed = true;
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return BadRequest(updateResult.Errors);
            }

            // Update user role
            var currentRoles = await _userManager.GetRolesAsync(user);
            var isAdmin = currentRoles.Contains("Administrator");

            if (userDto.IsAdmin && !isAdmin)
            {
                // Add to admin role
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, "Administrator");
            }
            else if (!userDto.IsAdmin && isAdmin)
            {
                // Remove from admin role
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, "User");
            }

            // Update password if requested
            if (userDto.SetNewPassword && !string.IsNullOrEmpty(userDto.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await _userManager.ResetPasswordAsync(user, token, userDto.NewPassword);

                if (!resetResult.Succeeded)
                {
                    return BadRequest(resetResult.Errors);
                }
            }

            return Ok(new { Message = "User updated successfully" });
        }

        // PATCH: api/admin/users/{id}/roles
        [HttpPatch("users/{id}/roles")]
        public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Get current roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove current roles
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            // Add new roles (only if they exist)
            var validRoles = new List<string>();
            foreach (var role in roles)
            {
                if (await _roleManager.RoleExistsAsync(role))
                {
                    validRoles.Add(role);
                }
            }

            if (validRoles.Any())
            {
                await _userManager.AddToRolesAsync(user, validRoles);
            }

            return Ok(new { Message = "User roles updated successfully" });
        }

        // POST: api/admin/users/{id}/reset-password
        [HttpPost("users/{id}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Generate a temporary password
            var tempPassword = "Temp" + Guid.NewGuid().ToString().Substring(0, 6) + "!";

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, tempPassword);

            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new
            {
                Message = $"Password for {user.Email} reset successfully",
                TempPassword = tempPassword
            });
        }

        // GET: api/admin/users/report
        [HttpGet("users/report")]
        public async Task<IActionResult> DownloadUserReport()
        {
            try
            {
                // Get all users with their quiz attempts
                var users = await _userManager.Users
                    .Include(u => u.QuizAttempts)
                    .ToListAsync();

                var userReportData = new List<UserReportData>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var roleName = roles.Any() ? string.Join(", ", roles) : "No Role";

                    // Get quiz statistics
                    var completedQuizzes = user.QuizAttempts.Count(a => a.EndTime != null);
                    var averageScore = user.QuizAttempts
                        .Where(a => a.EndTime != null)
                        .Select(a => a.Score)
                        .DefaultIfEmpty(0)
                        .Average();

                    // Get last login timestamp
                    var lastLoginFormatted = user.LastLoginTime.ToString("yyyy-MM-dd HH:mm:ss");

                    userReportData.Add(new UserReportData
                    {
                        UserId = user.Id,
                        Username = user.UserName ?? "N/A",
                        Email = user.Email ?? "N/A",
                        Role = roleName,
                        LastLoginTime = lastLoginFormatted,
                        QuizAttempts = completedQuizzes,
                        AverageScore = Math.Round(averageScore, 1),
                        IsEmailConfirmed = user.EmailConfirmed ? "Yes" : "No"
                    });
                }

                // Create CSV content
                var csv = new StringBuilder();
                csv.AppendLine("User ID,Username,Email,Role,Last Login,Quiz Attempts,Average Score (%),Email Confirmed");

                foreach (var userData in userReportData)
                {
                    csv.AppendLine($"\"{userData.UserId}\",\"{userData.Username}\",\"{userData.Email}\",\"{userData.Role}\",\"{userData.LastLoginTime}\",{userData.QuizAttempts},{userData.AverageScore},\"{userData.IsEmailConfirmed}\"");
                }

                // Encode to UTF8 bytes
                byte[] bytes = Encoding.UTF8.GetBytes(csv.ToString());

                // Return as file download
                return File(bytes, "text/csv", $"user-report-{DateTime.Now:yyyy-MM-dd}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating user report");
                return StatusCode(500, new { Message = "An error occurred while generating the report" });
            }
        }

        // DELETE: api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Don't allow admins to delete themselves
            if (User.Identity?.Name == user.UserName)
            {
                return BadRequest("You cannot delete your own account");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            return Ok(new { Message = "User deleted successfully" });
        }

        // POST: api/admin/users/bulk-delete
        [HttpPost("users/bulk-delete")]
        public async Task<IActionResult> BulkDeleteUsers([FromBody] List<string> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return BadRequest("No users selected for deletion");
            }

            var currentUserName = User.Identity?.Name;
            var successCount = 0;
            var errorCount = 0;
            var currentUserSelected = false;
            var errors = new List<string>();

            foreach (var userId in userIds)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    continue;
                }

                // Skip if trying to delete current user
                if (user.UserName == currentUserName)
                {
                    currentUserSelected = true;
                    continue;
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    successCount++;
                }
                else
                {
                    errorCount++;
                    errors.AddRange(result.Errors.Select(e => e.Description));
                }
            }

            var responseMessage = new StringBuilder();
            if (successCount > 0)
            {
                responseMessage.Append($"Successfully deleted {successCount} user(s). ");
                if (errorCount > 0)
                {
                    responseMessage.Append($"Failed to delete {errorCount} user(s). ");
                }
                if (currentUserSelected)
                {
                    responseMessage.Append("Your own account was not deleted. ");
                }
            }
            else if (currentUserSelected && errorCount == 0)
            {
                responseMessage.Append("Error: You cannot delete your own account.");
            }
            else
            {
                responseMessage.Append($"Error: Failed to delete {errorCount} user(s).");
            }

            return Ok(new
            {
                Message = responseMessage.ToString().Trim(),
                SuccessCount = successCount,
                ErrorCount = errorCount,
                Errors = errors
            });
        }

        // POST: api/admin/quizzes
        [HttpPost("quizzes")]
        public async Task<ActionResult<QuizDto>> CreateQuiz([FromBody] CreateQuizDto quizDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var quiz = new Quiz
            {
                Title = quizDto.Title,
                Description = quizDto.Description,
                TimeLimit = quizDto.TimeLimit,
                CreatedAt = DateTime.Now,
                Questions = new List<Question>()
            };

            foreach (var questionDto in quizDto.Questions)
            {
                var question = new Question
                {
                    Text = questionDto.Text,
                    Options = new List<Option>()
                };

                // Create options and mark the correct one
                foreach (var optionDto in questionDto.Options)
                {
                    question.Options.Add(new Option
                    {
                        Text = optionDto.Text,
                        IsCorrect = optionDto.IsCorrect
                    });
                }

                quiz.Questions.Add(question);
            }

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            // Return a simple DTO with the new quiz ID
            return CreatedAtAction(
                nameof(GetQuiz),
                new { id = quiz.Id },
                new QuizDto
                {
                    Id = quiz.Id,
                    Title = quiz.Title,
                    Description = quiz.Description,
                    TimeLimit = quiz.TimeLimit,
                    CreatedAt = quiz.CreatedAt,
                    QuestionCount = quiz.Questions.Count
                });
        }

        // GET: api/admin/quizzes/{id}
        [HttpGet("quizzes/{id}")]
        public async Task<ActionResult<AdminQuizDetailDto>> GetQuiz(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            var quizDto = new AdminQuizDetailDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                Description = quiz.Description,
                TimeLimit = quiz.TimeLimit,
                CreatedAt = quiz.CreatedAt,
                QuestionCount = quiz.Questions.Count,
                Questions = quiz.Questions.Select(q => new AdminQuestionDto
                {
                    Id = q.Id,
                    Text = q.Text,
                    Options = q.Options.Select(o => new AdminOptionDto
                    {
                        Id = o.Id,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                }).ToList()
            };

            return Ok(quizDto);
        }

        // PUT: api/admin/quizzes/{id}
        [HttpPut("quizzes/{id}")]
        public async Task<IActionResult> UpdateQuiz(int id, [FromBody] UpdateQuizDto quizDto)
        {
            if (id != quizDto.Id)
            {
                return BadRequest("Quiz ID mismatch");
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
            {
                return NotFound();
            }

            // Update basic quiz properties
            quiz.Title = quizDto.Title;
            quiz.Description = quizDto.Description;
            quiz.TimeLimit = quizDto.TimeLimit;

            // Handle questions - first identify which ones should be deleted
            var existingQuestionIds = quiz.Questions.Select(q => q.Id).ToList();
            var updatedQuestionIds = quizDto.Questions.Where(q => q.Id > 0).Select(q => q.Id).ToList();
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
            foreach (var questionDto in quizDto.Questions)
            {
                if (questionDto.Id > 0)
                {
                    // Update existing question
                    var existingQuestion = quiz.Questions.FirstOrDefault(q => q.Id == questionDto.Id);
                    if (existingQuestion != null)
                    {
                        existingQuestion.Text = questionDto.Text;

                        // Handle options for this question
                        var existingOptionIds = existingQuestion.Options.Select(o => o.Id).ToList();
                        var updatedOptionIds = questionDto.Options.Where(o => o.Id > 0).Select(o => o.Id).ToList();
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
                        foreach (var optionDto in questionDto.Options)
                        {
                            if (optionDto.Id > 0)
                            {
                                // Update existing option
                                var existingOption = existingQuestion.Options.FirstOrDefault(o => o.Id == optionDto.Id);
                                if (existingOption != null)
                                {
                                    existingOption.Text = optionDto.Text;
                                    existingOption.IsCorrect = optionDto.IsCorrect;
                                }
                            }
                            else
                            {
                                // Add new option
                                existingQuestion.Options.Add(new Option
                                {
                                    Text = optionDto.Text,
                                    IsCorrect = optionDto.IsCorrect,
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
                        Text = questionDto.Text,
                        QuizId = quiz.Id,
                        Options = new List<Option>()
                    };

                    foreach (var optionDto in questionDto.Options)
                    {
                        newQuestion.Options.Add(new Option
                        {
                            Text = optionDto.Text,
                            IsCorrect = optionDto.IsCorrect
                        });
                    }

                    quiz.Questions.Add(newQuestion);
                }
            }

            // Save changes
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { Message = "Quiz updated successfully" });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!QuizExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // DELETE: api/admin/quizzes/{id}
        [HttpDelete("quizzes/{id}")]
        public async Task<IActionResult> DeleteQuiz(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .Include(q => q.Attempts)
                .ThenInclude(a => a.Answers)
                .FirstOrDefaultAsync(q => q.Id == id);

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

            return Ok(new { Message = "Quiz deleted successfully" });
        }

        // GET: api/admin/stats
        [HttpGet("stats")]
        public async Task<ActionResult<AdminStatsDto>> GetStats()
        {
            var userCount = await _userManager.Users.CountAsync();
            var quizCount = await _context.Quizzes.CountAsync();
            var attemptCount = await _context.QuizAttempts.CountAsync();
            var averageScore = await _context.QuizAttempts
                .Where(a => a.EndTime != null)
                .AverageAsync(a => a.Score);

            // Get recent activity
            var recentAttempts = await _context.QuizAttempts
                .Include(a => a.User)
                .Include(a => a.Quiz)
                .Where(a => a.EndTime != null)
                .OrderByDescending(a => a.EndTime)
                .Take(10)
                .Select(a => new RecentAttemptDto
                {
                    AttemptId = a.Id,
                    UserId = a.UserId,
                    UserName = a.User.UserName,
                    QuizId = a.QuizId,
                    QuizTitle = a.Quiz.Title,
                    Score = a.Score,
                    EndTime = a.EndTime.Value
                })
                .ToListAsync();

            // Get popular quizzes
            var popularQuizzes = await _context.Quizzes
                .Include(q => q.Attempts)
                .Where(q => q.Attempts.Any())
                .Select(q => new PopularQuizDto
                {
                    Id = q.Id,
                    Title = q.Title,
                    AttemptCount = q.Attempts.Count,
                    AverageScore = q.Attempts.Any() ? q.Attempts.Average(a => a.Score) : 0
                })
                .OrderByDescending(q => q.AttemptCount)
                .Take(5)
                .ToListAsync();

            var statsDto = new AdminStatsDto
            {
                UserCount = userCount,
                QuizCount = quizCount,
                AttemptCount = attemptCount,
                AverageScore = averageScore,
                RecentAttempts = recentAttempts,
                PopularQuizzes = popularQuizzes
            };

            return Ok(statsDto);
        }

        private bool QuizExists(int id)
        {
            return _context.Quizzes.Any(e => e.Id == id);
        }
    }

    // Additional DTOs for admin operations
    public class UpdateUserDto
    {
        [Required]
        public string Id { get; set; }

        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Is Administrator")]
        public bool IsAdmin { get; set; }

        [Display(Name = "Set New Password")]
        public bool SetNewPassword { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }
    }

    public class UserReportData
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string LastLoginTime { get; set; } = string.Empty;
        public int QuizAttempts { get; set; }
        public double AverageScore { get; set; }
        public string IsEmailConfirmed { get; set; } = string.Empty;
    }

    public class CreateQuizDto
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        [Range(1, 120)]
        public int TimeLimit { get; set; }

        [Required]
        [MinLength(1)]
        public List<AdminQuestionDto> Questions { get; set; } = new List<AdminQuestionDto>();
    }

    public class UpdateQuizDto : CreateQuizDto
    {
        public int Id { get; set; }
    }

    public class AdminQuizDetailDto : QuizDto
    {
        public List<AdminQuestionDto> Questions { get; set; } = new List<AdminQuestionDto>();
    }

    public class AdminQuestionDto
    {
        public int Id { get; set; }

        [Required]
        public string Text { get; set; }

        [Required]
        [MinLength(2)]
        public List<AdminOptionDto> Options { get; set; } = new List<AdminOptionDto>();
    }

    public class AdminOptionDto
    {
        public int Id { get; set; }

        [Required]
        public string Text { get; set; }

        public bool IsCorrect { get; set; }
    }

    public class AdminStatsDto
    {
        public int UserCount { get; set; }
        public int QuizCount { get; set; }
        public int AttemptCount { get; set; }
        public double AverageScore { get; set; }
        public List<RecentAttemptDto> RecentAttempts { get; set; } = new List<RecentAttemptDto>();
        public List<PopularQuizDto> PopularQuizzes { get; set; } = new List<PopularQuizDto>();
    }

    public class RecentAttemptDto
    {
        public int AttemptId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public int Score { get; set; }
        public DateTime EndTime { get; set; }
    }

    public class PopularQuizDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int AttemptCount { get; set; }
        public double AverageScore { get; set; }
    }
}