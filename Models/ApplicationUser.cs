using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace QuizApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime LastLoginTime { get; set; }

        // Initialize the collection to avoid null reference warnings
        public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    }
}