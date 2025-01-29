using InterviewWizard.Models.Session;
using InterviewWizard.Models.User;
using InterviewWizard.Models.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InterviewWizard.Pages
{
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    public class ProfileModel : PageModel
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public User? _thisUser;

        public ProfileModel(ApplicationDbContext dbcontext, IHttpContextAccessor httpContextAccessor)
        {
            _dbcontext = dbcontext;
            _httpContextAccessor = httpContextAccessor;
        }
        public async Task<IActionResult> OnGet()
        {
            string? sessionKey = _httpContextAccessor.HttpContext?.Session.GetString("SessionKey");
            if (sessionKey == null)
            {
                return RedirectToPage("/login");
            }
            else
            {
                Session? thisSession = await _dbcontext.Sessions.Where(s => s.SessionKey == sessionKey).FirstOrDefaultAsync();
                if (thisSession == null)
                {
                    return RedirectToPage("/login");
                }
                User? thisUser = await _dbcontext.Users.Where(u => u.Id == thisSession.UserId).FirstOrDefaultAsync();
                if (thisUser == null)
                {
                    return RedirectToPage("/error?err=profilenotfound");
                }  
                _thisUser = thisUser;
                return Page();
            }
        }
    }
}
