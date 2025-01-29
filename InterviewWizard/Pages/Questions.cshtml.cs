using InterviewWizard.Helpers;
using InterviewWizard.Models.Response;
using InterviewWizard.Models.Session;
using InterviewWizard.Models.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace InterviewWizard.Pages
{
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    public class QuestionsModel : PageModel
    {
        private readonly SessionService _sessionService;
        
        public string? QuestionId { get; set; }
        public string? QuestionText { get; set; }

        [FromRoute]
        public int Id { get; set; }
        
        public QuestionsModel(ApplicationDbContext dbcontext, SessionService sessionService)
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
            Question? q = thisSession.Questions.Where(q => q.QuestionNumber == id).FirstOrDefault();
            if (q == null)
            {
                var errorRedirect = new RedirectToPageResult("/error?err=questionnotfound");
                return errorRedirect;
            }
            QuestionId = q.Id.ToString("N");
            QuestionText = q.QuestionText;
            return Page();
        }
    }
}
