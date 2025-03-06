using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuizApp.Models;

namespace QuizApp.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Check if roles already exist before trying to create them
            if (!await context.Roles.AnyAsync())
            {
                string[] roleNames = { "Administrator", "User" };
                foreach (var roleName in roleNames)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create admin user if it doesn't exist
            var adminEmail = "admin@quizapp.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    LastLoginTime = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                }
            }

            // Create sample quiz if none exist
            if (!await context.Quizzes.AnyAsync())
            {
                var quiz = new Quiz
                {
                    Title = "Sample Quiz",
                    Description = "This is a sample quiz to get you started.",
                    TimeLimit = 10,
                    CreatedAt = DateTime.Now,
                    Questions = new List<Question>
                    {
                        new Question
                        {
                            Text = "What is the capital of France?",
                            Options = new List<Option>
                            {
                                new Option { Text = "London", IsCorrect = false },
                                new Option { Text = "Berlin", IsCorrect = false },
                                new Option { Text = "Paris", IsCorrect = true },
                                new Option { Text = "Rome", IsCorrect = false }
                            }
                        },
                        new Question
                        {
                            Text = "Which planet is closest to the sun?",
                            Options = new List<Option>
                            {
                                new Option { Text = "Venus", IsCorrect = false },
                                new Option { Text = "Mercury", IsCorrect = true },
                                new Option { Text = "Earth", IsCorrect = false },
                                new Option { Text = "Mars", IsCorrect = false }
                            }
                        }
                    }
                };

                context.Quizzes.Add(quiz);
                await context.SaveChangesAsync();
            }
        }
    }
}