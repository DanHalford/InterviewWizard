using InterviewWizard.Helpers;
using InterviewWizard.Models.Session;
using InterviewWizard.Models.User;
using InterviewWizard.Models.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InterviewWizard.Pages
{
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    public class DocumentsModel : PageModel
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly SessionService _sessionService;

        public DocumentsModel(ApplicationDbContext dbcontext, SessionService sessionService)
        {
            _dbcontext = dbcontext;
            _sessionService = sessionService;
        }
        public async Task<IActionResult> OnGet()
        {
            Session? thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);
            if (thisSession == null)
            {
                return RedirectToPage("/error?err=sessionnotfound");
            }
            User? thisUser = _dbcontext.Users.Where(u => u.Id == thisSession.UserId).FirstOrDefault();
            if (thisUser == null)
            {
                return RedirectToPage("/error?err=usernotfound");
            }
            if (thisUser.SubscriptionType == SubscriptionType.Free && thisUser.Credits < 1)
            {
                return RedirectToPage("/launch");
            }
            return Page();
        }
    }
}
