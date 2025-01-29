using InterviewWizard.Helpers;
using InterviewWizard.Models.Session;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InterviewWizard.Pages
{
    public class QuestionsPreparingModel : PageModel
    {

        private readonly SessionService _sessionService;

        public QuestionsPreparingModel(SessionService sessionService)
        {
            _sessionService = sessionService;
        }
        public async Task<IActionResult> OnGet(int id)
        {
            Session thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);
            if (thisSession == null)
            {
                var errorRedirect = new RedirectToPageResult("/error?err=sessionnotfound");
                return errorRedirect;
            }
            return Page();
        }
    }
}
