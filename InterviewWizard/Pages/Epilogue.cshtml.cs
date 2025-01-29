using Azure.Storage.Blobs;
using DocumentFormat.OpenXml.Office2010.Excel;
using InterviewWizard.Helpers;
using InterviewWizard.Models.Session;
using InterviewWizard.Models.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InterviewWizard.Pages
{
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    public class EpilogueModel(SessionService sessionService, ApplicationDbContext dbContext) : PageModel
    {
        private readonly SessionService _sessionService = sessionService;
        private readonly ApplicationDbContext _dbcontext = dbContext;

        public string? Feedback { get; set; }

        public async Task<IActionResult> OnGet()
        {
            Session thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);
            if (thisSession == null)
            {
                var errorRedirect = new RedirectToPageResult("/error?err=sessionnotfound");
                return errorRedirect;
            }
            Feedback = thisSession.Epilogue;
            return Page();
        }
    }
}
