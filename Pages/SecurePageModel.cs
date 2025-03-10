using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QuizApp.Pages
{
    public abstract class SecurePageModel : PageModel
    {
        // Override the OnPageHandlerExecuting method to check authentication state
        public override void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context)
        {
            // For pages that require authentication, verify the user is still authenticated
            if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
            {
                // If not authenticated and this is an AJAX request
                if (context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Result = new UnauthorizedResult();
                    return;
                }

                // For normal requests, redirect to login
                context.Result = new RedirectToPageResult("/Account/Login", new 
                { 
                    area = "Identity", 
                    returnUrl = context.HttpContext.Request.Path 
                });
                return;
            }

            base.OnPageHandlerExecuting(context);
        }
    }
}