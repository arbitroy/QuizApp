using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuizApp.Pages;

public class PrivacyModel(ILogger<PrivacyModel> logger) : PageModel
{
    private readonly ILogger<PrivacyModel> _logger = logger;
    
    public void OnGet()
    {
        _logger.LogInformation("Privacy page accessed at {Time}", DateTime.UtcNow);
    }
}