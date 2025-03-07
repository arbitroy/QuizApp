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
        public string Title { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        [Required]
        public int TimeLimit { get; set; } // Time limit in minutes
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        // Initialize collections to empty lists to avoid null reference warnings
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
        public virtual ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
    }

    public class Question
    {
        public int Id { get; set; }
        
        [Required]
        public string Text { get; set; } = string.Empty;
        
        public int QuizId { get; set; }
        public virtual Quiz Quiz { get; set; } = null!;
        
        public virtual ICollection<Option> Options { get; set; } = new List<Option>();
    }

    public class Option
    {
        public int Id { get; set; }
        
        [Required]
        public string Text { get; set; } = string.Empty;
        
        public bool IsCorrect { get; set; }
        
        public int QuestionId { get; set; }
        public virtual Question Question { get; set; } = null!;
    }

    public class QuizAttempt
    {
        public int Id { get; set; }
        
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        
        public int QuizId { get; set; }
        public virtual Quiz Quiz { get; set; } = null!;
        
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        public int Score { get; set; }
        
        public virtual ICollection<UserAnswer> Answers { get; set; } = new List<UserAnswer>();
    }

    public class UserAnswer
    {
        public int Id { get; set; }
        
        public int QuizAttemptId { get; set; }
        public virtual QuizAttempt QuizAttempt { get; set; } = null!;
        
        public int QuestionId { get; set; }
        public virtual Question Question { get; set; } = null!;
        
        public int SelectedOptionId { get; set; }
        public virtual Option SelectedOption { get; set; } = null!;
    }
}