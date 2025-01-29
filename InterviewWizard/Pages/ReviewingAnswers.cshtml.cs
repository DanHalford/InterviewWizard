using InterviewWizard.Models.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InterviewWizard.Pages
{
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    public class ReviewingAnswersModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
