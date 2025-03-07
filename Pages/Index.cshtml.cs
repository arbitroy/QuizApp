using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Pages.Quizzes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizApp.Pages
{
    public class IndexModel(ApplicationDbContext context) : PageModel
    {
        public IList<Quiz> RecentQuizzes { get; set; } = new List<Quiz>();

        public async Task OnGetAsync()
        {
            // Get 3 most recent quizzes
            RecentQuizzes = await context.Quizzes
                .OrderByDescending(q => q.CreatedAt)
                .Take(3)
                .ToListAsync();
        }
    }
}