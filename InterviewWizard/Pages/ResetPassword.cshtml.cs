using DocumentFormat.OpenXml.Presentation;
using InterviewWizard.Helpers;
using InterviewWizard.Models.Request;
using InterviewWizard.Models.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace InterviewWizard.Pages
{
    public class ResetPasswordModel : PageModel
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ResetPasswordStatus status = ResetPasswordStatus.Start;
        public string? _token = null;

        public ResetPasswordModel(ApplicationDbContext dbcontext, IHttpContextAccessor httpContextAccessor)
        {
            _dbcontext = dbcontext;
            _httpContextAccessor = httpContextAccessor;
        }

        public IActionResult OnGet(string? token = null)
        {
            if (token == null) 
            {
                return Page();
            } else
            {
                Guid tokenGuid = Guid.Parse(token);
                ResetRequest? resetRequest = _dbcontext.ResetRequests.Where(r => r.Token == tokenGuid).FirstOrDefault();
                if (resetRequest == null)
                {
                    status = ResetPasswordStatus.InvalidToken;
                    return Page();
                }
                if (resetRequest.Created < DateTime.UtcNow.AddMinutes(-30))
                {
                    status = ResetPasswordStatus.ExpiredToken;
                    return Page();
                }
                status = ResetPasswordStatus.Password;
                _token = token;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync(PasswordResetRequest prr)
        {
            if (prr.Token == null)
            {
                return Page();
            }
            else if (prr.Password == null || prr.Password.Length < 14)
            {
                status = ResetPasswordStatus.PasswordTooShort;
                return Page();  
            }
            else
            {
                Guid tokenGuid = Guid.Parse(prr.Token);
                ResetRequest? resetRequest = _dbcontext.ResetRequests.Where(r => r.Token == tokenGuid).FirstOrDefault();
                if (resetRequest == null)
                {
                    status = ResetPasswordStatus.InvalidToken;
                    return Page();
                }
                if (resetRequest.Created < DateTime.UtcNow.AddMinutes(-30))
                {
                    status = ResetPasswordStatus.ExpiredToken;
                    return Page();
                }

                UserService us = new UserService(_dbcontext);
                User user = await us.GetUserByEmailAsync(resetRequest.Email);
                user.Password = await Models.User.User.CreatePasswordHash(prr.Password, user.Salt);
                await us.UpdateUserAsync(user);
                resetRequest.Used = true;
                await _dbcontext.SaveChangesAsync();
                status = ResetPasswordStatus.Success;
                return Page();
            }
        }
    }
}
