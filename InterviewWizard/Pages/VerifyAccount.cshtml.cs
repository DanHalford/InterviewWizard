using InterviewWizard.Models.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using InterviewWizard.Models.Response;
using InterviewWizard.Helpers;

namespace InterviewWizard.Pages
{
    public class VerifyAccountModel(ApplicationDbContext dbcontext, ILogger<VerifyAccountModel> logger) : PageModel
    {
        private readonly ApplicationDbContext _dbcontext = dbcontext;
        private readonly ILogger<VerifyAccountModel> _logger = logger;

        public bool showCodeBox { get; set; } = true;
        public string _token { get; set; } = string.Empty;
        public APIResponseModel? response { get; set; }

        public void OnGet([FromQuery] string? token = null)
        {
            _logger.BeginScope("VerifyAccount - OnGet");
            if (string.IsNullOrEmpty(token))
            {
                //Bad request
            }
            _token = token ?? string.Empty;
            if (response == null)
            {
                _logger.LogInformation("Response is null");
                showCodeBox = true;
            } else if (response != null && response.Success == false)
            {
                _logger.LogInformation("Response is not null and is not successful");
                showCodeBox = true;
            } else {
                showCodeBox = false;
            }
        }
   
        // Things are slightly confusing here.
        // The "code" parameter is the six-digit code included in the verification email.
        // The "token" parameter is the user's unique identifier. The terms are used in the querystring
        // to avoid referring to the UserId as a UserId.
        public IActionResult OnPost(string? code = null, string? token = null)
        {

            _logger.BeginScope("VerifyAccount - OnPost");
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Token is null or empty");
                return RedirectToPage("/error?err=invalidtoken");
            }
            if (string.IsNullOrEmpty(code)) {
                showCodeBox = true;
                return Page();
            }
            token = token ?? string.Empty;
            showCodeBox = false;
            UserService us = new UserService(_dbcontext);
            Guid UserId = new Guid(token);
            try
            {
                User user = us.GetUserAsync(token).Result;
                VerifyRequest vr = _dbcontext.VerifyRequests.First(r => r.UserId == UserId);

                if (user.Verified) {
                    response = new APIResponseModel {Success = true, Message = "ALREADYVERIFIED" };
                } else if (vr == null) {
                    response = new APIResponseModel { Success = false, Message = "INVALIDATTEMPT" };
                } else if (vr.Token == code) {
                    user.Verified = true;
                    vr.Used = true;
                    _dbcontext.SaveChanges();
                    response = new APIResponseModel { Success = true };
                } else {
                    response = new APIResponseModel { Success = false, Message = "INVALIDATTEMPT" };
                }
            }
            catch
            {
                response = new APIResponseModel { Success = false, Message = "INVALIDATTEMPT" };
            }
            switch (response.Message)
            {
                case "ALREADYVERIFIED":
                    response.Message = "This account has already been verified. Please login to continue.";
                    break;
                case "INVALIDATTEMPT":
                    response.Message = "The code entered does not appear to be correct. Please check and try again.";
                    break;
                default:
                    response.Message = "Account verified successfully. Please login to continue.";
                    break;
            }
            return Page();
        }
    }
}
