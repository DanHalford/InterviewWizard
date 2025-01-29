using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using InterviewWizard.Models.Session;
using Microsoft.Graph;
using Microsoft.EntityFrameworkCore;
using AspNetCore.ReCaptcha;

using User = InterviewWizard.Models.User.User;
using InterviewWizard.Models.Utility;
using InterviewWizard.Models.Request;
using InterviewWizard.Models.User;
using InterviewWizard.Models.Response;
using InterviewWizard.Helpers;

namespace InterviewWizard.Controllers
{
    /// <summary>
    /// Controller for authentication and user management
    /// </summary>
    [Route("auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly AppSettings _appSettings;
        private readonly ApplicationDbContext _dbcontext;
        private readonly SessionService _sessionService;
        private readonly GraphServiceClient _graphClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthController> _logger;
        private readonly IReCaptchaService _reCaptchaService;

        public AuthController(
            BlobServiceClient blobServiceClient,
            BlobContainerClient blobContainerClient,
            IOptions<AppSettings> appSettings,
            ApplicationDbContext dbcontext,
            SessionService sessionService,
            GraphServiceClient graphServiceClient,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuthController> logger,
            IReCaptchaService reCaptchaService)
        {
            _blobServiceClient = blobServiceClient;
            _blobContainerClient = blobContainerClient;
            _appSettings = appSettings.Value;
            _dbcontext = dbcontext;
            _sessionService = sessionService;
            _graphClient = graphServiceClient;
            _httpContextAccessor = httpContextAccessor;
            _reCaptchaService = reCaptchaService;
            _logger = logger;
            _logger.LogDebug("AuthController created");
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="request">A <see cref="RegisterUserRequestModel">RegisterUserModel</c> object</param>
        /// <returns>
        /// If password is too short, returns HTTP 400 PASSWORDLENGTH
        /// If user already exists, returns HTTP 400 USEREXISTS
        /// Otherwise, returns HTTP 201
        /// </returns>
        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequestModel request)
        {
            _logger.BeginScope("Registering new user {email}", request.Email);
            _logger.LogInformation("Validating reCaptcha");

            //Not processing Captcha at present as token doesn't appear to be working. See: https://github.com/michaelvs97/AspNetCore.ReCaptcha/issues/66
            var reCaptchaResponse = await _reCaptchaService.GetVerifyResponseAsync(request.token);

            if (request.Password.Length < 14) {
                _logger.LogInformation("Password too short");
                return BadRequest("PASSWORDLENGTH");
            }
            UserService us = new UserService(_dbcontext);
            try
            {
                await us.GetUserByEmailAsync(request.Email);
                _logger.LogInformation("User already exists");
                return BadRequest("USEREXISTS");
            }
            catch 
            {
                // User does not exist
            }
            _logger.LogInformation("Creating new user");
            var salt = InterviewWizard.Models.User.User.CreateSalt();
            var user = new User
            {
                Email = request.Email,
                Salt = salt,
                Password = await InterviewWizard.Models.User.User.CreatePasswordHash(request.Password, salt),
                SubscriptionType = SubscriptionType.Free,
                Verified = false,
                Created = DateTime.UtcNow,
                Credits = 5
            };

            _dbcontext.Users.Add(user);
            await _dbcontext.SaveChangesAsync();

            _logger.LogInformation("Creating verification request record");
            Random random = new Random();
            int token = random.Next(100000, 999999);
            VerifyRequest vr = new VerifyRequest
            {
                User = user,
                Token = token.ToString(),
                Created = DateTime.UtcNow,
                Used = false
            };
            _dbcontext.VerifyRequests.Add(vr);
            await _dbcontext.SaveChangesAsync();

            _logger.LogInformation("Sending verification email");
            string templateName = _appSettings.Email.Templates["VerifyEmail"] + ".html";
            string emailTemplate = System.IO.File.ReadAllText($"Templates/{templateName}");
            var url = $"https://interviewwizard.ai/verify-account?code={token.ToString()}&token={user.Id.ToString("N")}";
            emailTemplate = emailTemplate.Replace("{{ url }}", url).Replace("{{ token }}", token.ToString());
            EmailService emailService = new EmailService(_graphClient, _appSettings);
            await emailService.SendEmailAsync(request.Email, "New account - verify your email", emailTemplate);

            _logger.LogInformation("User registered and verification email sent");
            Response.Headers.Append("Token", user.Id.ToString("N"));
            return Created("", new APIResponseModel { Success = true, Message = user.Id.ToString("N") });
        }

        /// <summary>
        /// Confirms the existence of a registered user with the specified email address
        /// </summary>
        /// <param name="email">Email address to check</param>
        /// <returns></returns>
        [HttpGet]
        [Route("{email}/exists")]
        public async Task<IActionResult> Exists([FromRoute] string email)
        {
            _logger.BeginScope("Checking existence of user with email {email}", email);
            UserService us = new UserService(_dbcontext);
            
            _logger.LogInformation("Simulating delay to prevent enumeration attacks");
            await Task.Delay(1000);

            try
            {
                _logger.LogInformation("Checking for user");
                await us.GetUserByEmailAsync(email);
                _logger.LogInformation("User found");
                return Ok();
            }
            catch
            {
                _logger.LogInformation("User not found");
                return NoContent();
            }
        }

        /// <summary>
        /// Processes a user login attempt
        /// </summary>
        /// <param name="request">A <see cref="RegisterUserRequestModel">RegisterUserModel</c> object</param>
        /// <returns>
        /// Returns HTTP 400 NOTVERIFIED if the username and password are correct but the user has not verified their account
        /// Returns HTTP 400 INVALIDLOGIN if the username and password are incorrect
        /// Returns HTTP 200 OK if the username and password are correct
        /// </returns>
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequestModel request)
        {
            UserService us = new UserService(_dbcontext);
            try
            {
                _logger.BeginScope("Login attempt for {email}", request.Email);
                User user = await us.GetUserByEmailAsync(request.Email);
                _logger.LogInformation($"User found with id: {user.Id}");
                _logger.LogInformation("Verifying password");
                if (await user.VerifyPassword(request.Password))
                {
                    _logger.LogInformation("Password accepted");
                    if (user.Verified) {
                        user.LastLogin = DateTime.UtcNow;
                        await us.UpdateUserAsync(user);
                        _logger.LogInformation("User lastlogin updated");
                        string? sessionKey = _httpContextAccessor.HttpContext?.Session.GetString("SessionKey");
                        if (sessionKey == null)
                        {
                            sessionKey = Guid.NewGuid().ToString();
                            _httpContextAccessor.HttpContext?.Session.SetString("SessionKey", sessionKey);
                        }
                        _logger.LogInformation($"Session key: {sessionKey}");
                        Session thisSession = await _sessionService.GetSessionAsync(sessionKey);
                        if (thisSession == null)
                        {
                            _logger.LogInformation("Session not found. Creating new session.");
                            Session session = SessionFactory.CreateSession(sessionKey);
                            await _sessionService.CreateSessionAsync(session);
                            thisSession = session;
                        }

                        thisSession.User = user;
                        await _sessionService.UpdateSessionAsync(thisSession);
                        _logger.LogInformation("Session updated with user details");
                        Response.Headers.Append("Authorization", thisSession.SessionKey);
                        return Ok(new AuthResponseModel { Success = true, Message = null, Token = thisSession.SessionKey});
                    } else
                    {
                        _logger.LogInformation("Credentials accepted but user not verified");
                        return Unauthorized(new AuthResponseModel { Success = false, Message = "NOTVERIFIED" });
                    }
                }
                else
                {
                    _logger.LogInformation("Password rejected");
                    await Task.Delay(2000);
                    return Unauthorized(new AuthResponseModel { Success = false, Message = "INVALIDLOGIN" });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error during login attempt");
                await Task.Delay(2000);
                return Unauthorized(new AuthResponseModel { Success = false, Message = "INVALIDLOGIN" });
            }
        }

        /// <summary>
        /// Password Reset step 1: Triggers a reset password confirmation email
        /// </summary>
        /// <param name="emailAddress">The email address of the user whose password a reset is being requested</param>
        /// <returns>Always returns HTTP 200 to avoid confirmation of existence of user accounts</returns>
        [HttpPost]
        [Route("reset")]
        public async Task<IActionResult> Reset([FromBody] ResetStructure email)
        {
            UserService us = new UserService(_dbcontext);
            try
            {
                User user = await us.GetUserByEmailAsync(email.Email);
                var token = Guid.NewGuid();
                var resetRequest = new ResetRequest
                {
                    Email = email.Email,
                    Token = token,
                    Created = DateTime.UtcNow
                };
                _dbcontext.ResetRequests.Add(resetRequest);
                await _dbcontext.SaveChangesAsync();
                
                string templateName = _appSettings.Email.Templates["PasswordReset"] + ".html";
                string emailTemplate = System.IO.File.ReadAllText($"Templates/{templateName}");
                var url = $"https://interviewwizard.ai/reset-password?token={token.ToString("N")}"; // will need to change to the final UI password reset page
                emailTemplate = emailTemplate.Replace("{{ url }}", url);
                EmailService emailService = new EmailService(_graphClient, _appSettings);
                await emailService.SendEmailAsync(email.Email, "Password Reset", emailTemplate);
                return Ok();
            }
            catch
            {
                return Ok();
            }
        }

        /// <summary>
        /// Password Reset step 2: Sets new password
        /// </summary>
        /// <param name="token">The password reset verification token</param>
        /// <param name="password">The new password</param>
        /// <returns></returns>
        [HttpPut]
        [Route("reset/{token}")]
        public async Task<IActionResult> Reset([FromRoute] string token, [FromBody] PasswordStructure password)
        {
            if (password.Password.Length < 14) { return BadRequest(new APIResponseModel { Success = false, Message = "PASSWORDLENGTH" }); }
            ResetRequest rr = await _dbcontext.ResetRequests.FirstAsync(r => r.Token.ToString("N").ToLower() == token.ToLower());
            if (rr == null) { BadRequest(new APIResponseModel { Success = false, Message = "INVALIDATTEMPT" }); }
            if (rr.Created.AddHours(4) < DateTime.UtcNow) { return BadRequest(new APIResponseModel { Success = false, Message = "EXPIRED" }); } 

            UserService us = new UserService(_dbcontext);
            User user = await us.GetUserByEmailAsync(rr.Email);
            user.Password = await Models.User.User.CreatePasswordHash(password.Password, user.Salt);
            await us.UpdateUserAsync(user);
            rr.Used = true;
            await _dbcontext.SaveChangesAsync();
            return Ok();
        }

        [HttpGet]
        [Route("status")]
        public async Task<IActionResult> GetAuthStatus()
        {
            string? sessionKey = _sessionService.SessionKey ?? _httpContextAccessor.HttpContext?.Session.GetString("SessionKey") ?? null;
            if (sessionKey == null) { return Ok(new AuthStatus { IsAuthenticated = false }) ; }
            Session s = await _sessionService.GetSessionAsync(sessionKey);
            if (s == null) { return Ok(new AuthStatus { IsAuthenticated = false }); }
            return Ok(new AuthStatus { IsAuthenticated = true, Token = s.SessionKey, EmailAddress = s.User.Email });
        }
    }
}
