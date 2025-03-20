using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;
using QuizApp.Api.Controllers;

namespace QuizApp.Api.Models
{
    // DTOs for Quiz-related operations
    public class QuizDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int TimeLimit { get; set; }
        public DateTime CreatedAt { get; set; }
        public int QuestionCount { get; set; }
    }

    public class QuizDetailDto : QuizDto
    {
        public List<QuestionDto> Questions { get; set; } = new List<QuestionDto>();
    }

    public class QuestionDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public List<OptionDto> Options { get; set; } = new List<OptionDto>();
    }

    public class OptionDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }

    // DTO for submitting quiz answers
    public class QuizSubmissionDto
    {
        public int QuizId { get; set; }
        public Dictionary<int, int> Answers { get; set; } = new Dictionary<int, int>();
    }

    // DTO for quiz result
    public class QuizResultDto
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public int Score { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public List<QuestionResultDto> Questions { get; set; } = new List<QuestionResultDto>();
    }

    public class QuestionResultDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public int SelectedOptionId { get; set; }
        public string SelectedOptionText { get; set; }
        public int CorrectOptionId { get; set; }
        public string CorrectOptionText { get; set; }
        public bool IsCorrect { get; set; }
    }

    // DTOs for User operations
    public class UserDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
    }

    public class UserProfileDto : UserDto
    {
        public DateTime LastLoginTime { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public int QuizzesTaken { get; set; }
        public double AverageScore { get; set; }
    }

    // DTOs for Authentication
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; }
    }

    // DTO for dashboard and statistics
    public class UserStatsDto
    {
        public int TotalQuizzesTaken { get; set; }
        public double AverageScore { get; set; }
        public double BestScore { get; set; }
        public List<QuizAttemptSummaryDto> RecentAttempts { get; set; } = new List<QuizAttemptSummaryDto>();
        public List<CategoryStatsDto> CategoryPerformance { get; set; } = new List<CategoryStatsDto>();
    }

    public class QuizAttemptSummaryDto
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public string QuizTitle { get; set; }
        public int Score { get; set; }
        public DateTime EndTime { get; set; }
    }
}