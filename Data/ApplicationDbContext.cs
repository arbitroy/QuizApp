using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QuizApp.Models;

namespace QuizApp.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Quiz>? Quizzes { get; set; }
        public DbSet<Question>? Questions { get; set; }
        public DbSet<Option>? Options { get; set; }
        public DbSet<QuizAttempt>? QuizAttempts { get; set; }
        public DbSet<UserAnswer>? UserAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Quiz)
                .WithMany(qz => qz.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Option>()
                .HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAttempt>()
                .HasOne(qa => qa.User)
                .WithMany(u => u.QuizAttempts)
                .HasForeignKey(qa => qa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuizAttempt>()
                .HasOne(qa => qa.Quiz)
                .WithMany(q => q.Attempts)
                .HasForeignKey(qa => qa.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAnswer>()
                .HasOne(ua => ua.QuizAttempt)
                .WithMany(qa => qa.Answers)
                .HasForeignKey(ua => ua.QuizAttemptId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}