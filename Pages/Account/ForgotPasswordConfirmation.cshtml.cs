using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuizApp.Pages.Account
{
    public class ForgotPasswordConfirmationModel : PageModel
    {
        [TempData]
        public string ResetLink { get; set; } = string.Empty;

        public void OnGet()
        {
        }
    }
}