using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuizApp.Models
{
    public class Quiz
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string? Title { get; set; }
        
        public string? Description { get; set; }
        
        [Required]
        public int TimeLimit { get; set; } // Time limit in minutes
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public virtual ICollection<Question>? Questions { get; set; }
        public virtual ICollection<QuizAttempt>? Attempts { get; set; }
    }

    public class Question
    {
        public int Id { get; set; }
        
        [Required]
        public string? Text { get; set; }
        
        public int QuizId { get; set; }
        public virtual Quiz? Quiz { get; set; }
        
        public virtual ICollection<Option>? Options { get; set; }
    }

    public class Option
    {
        public int Id { get; set; }
        
        [Required]
        public string? Text { get; set; }
        
        public bool IsCorrect { get; set; }
        
        public int QuestionId { get; set; }
        public virtual Question? Question { get; set; }
    }

    public class QuizAttempt
    {
        public int Id { get; set; }
        
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        
        public int QuizId { get; set; }
        public virtual Quiz? Quiz { get; set; }
        
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        public int Score { get; set; }
        
        public virtual ICollection<UserAnswer>? Answers { get; set; }
    }

    public class UserAnswer
    {
        public int Id { get; set; }
        
        public int QuizAttemptId { get; set; }
        public virtual QuizAttempt? QuizAttempt { get; set; }
        
        public int QuestionId { get; set; }
        public virtual Question? Question { get; set; }
        
        public int SelectedOptionId { get; set; }
        public virtual Option? SelectedOption { get; set; }
    }
}