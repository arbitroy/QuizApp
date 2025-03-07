using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Pages.Quizzes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuizApp.Pages.Admin.Quizzes
{
    [Authorize(Policy = "RequireAdministratorRole")]
    public class IndexModel(ApplicationDbContext context) : PageModel
    {
        public IList<Quiz> Quizzes { get; set; } = new List<Quiz>();

        public async Task OnGetAsync()
        {
            Quizzes = await context.Quizzes
                .Include(q => q.Questions)
                .ToListAsync();
        }
    }
}