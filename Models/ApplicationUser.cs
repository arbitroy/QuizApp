using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace QuizApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime LastLoginTime { get; set; }

        public virtual ICollection<QuizAttempt>? QuizAttempts { get; set; }
    }
}