using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuizApp.Pages.Quizzes
{
    [Authorize]
    public class IndexModel(ApplicationDbContext context) : PageModel
    {
        public IList<Quiz>? Quizzes { get; set; }

        public async Task OnGetAsync()
        {
            Quizzes = await context.Quizzes.ToListAsync();
        }
    }
}
