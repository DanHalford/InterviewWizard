using InterviewWizard.Helpers;
using InterviewWizard.Models.Session;
using InterviewWizard.Models.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InterviewWizard.Pages
{
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    public class AnswerFeedbackModel(ApplicationDbContext dbcontext, SessionService sessionService) : PageModel
    {

        private readonly ApplicationDbContext _dbcontext = dbcontext;
        private readonly SessionService _sessionService = sessionService;

        public Question? QuestionObject { get; set; }

        [FromRoute]
        public int Id { get; set; }

        public async Task<IActionResult> OnGet(int id)
        {
            Session thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);
            if (thisSession == null)
            {
                var errorRedirect= new RedirectToPageResult("/error?err=sessionnotfound");
                return errorRedirect;
            }
            QuestionObject = thisSession.Questions.Where(q => q.QuestionNumber == id).FirstOrDefault();
            if (QuestionObject == null)
            {
                var errorRedirect = new RedirectToPageResult("/error?err=questionnotfound");
                return errorRedirect;
            }
            return Page();
        }
    }
}
