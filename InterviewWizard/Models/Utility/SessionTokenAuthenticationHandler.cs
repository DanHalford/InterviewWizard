using InterviewWizard.Controllers;
using InterviewWizard.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace InterviewWizard.Models.Utility
{
    public class SessionTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly SessionService _sessionService;
        private readonly ILogger<SessionTokenAuthenticationHandler> _logger;

        public const string SchemeName = "IWSessionToken";

        public SessionTokenAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            ISystemClock clock,
            ApplicationDbContext dbContext,
            SessionService sessionService,
            ILogger<SessionTokenAuthenticationHandler> logger) : base(options, loggerFactory, encoder, clock)
        {
            _dbContext = dbContext;
            _sessionService = sessionService;
            _logger = logger;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Session.Session session = null;
            string? sessionToken = null;
            
            _logger.BeginScope("Begin SessionTokenAuthenticationHandler");

            if (string.IsNullOrEmpty(_sessionService.SessionKey))
            {
                _logger.LogInformation("Session key not found");
                return AuthenticateResult.Fail("Session key not found");
            }

            if (Request.Path.StartsWithSegments("/api"))
            {
                _logger.LogDebug("API request");
                var token = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogInformation("Authorization token missing or empty");
                    return AuthenticateResult.Fail("Missing token");
                }
                string[] tokenComponents = token.Split(" ");
                if (tokenComponents == null || tokenComponents.Length != 2 || tokenComponents[0] != "Bearer")
                {
                    _logger.LogInformation($"Malformed token: {token}");
                    return AuthenticateResult.Fail("Malformed token");
                }
                sessionToken = tokenComponents[1];
                session = await _sessionService.GetSessionAsync(sessionToken);
            }
            else
            {
                _logger.LogDebug("Non-API request");
                sessionToken = _sessionService.SessionKey;
                session = await _sessionService.GetSessionAsync(_sessionService.SessionKey);
            }
            
            if (session == null)
            {
                _logger.LogInformation($"Session not found for token: {sessionToken}");
                return AuthenticateResult.Fail("Invalid token");
            }
            if (session.LastActive < DateTime.UtcNow.AddMinutes(-30))
            {
                _logger.LogInformation($"Session expired for token: {sessionToken}");
                return AuthenticateResult.Fail("Expired token");
            }
            if (session.UserId == null)
            {
                _logger.LogInformation($"Session has no user for token: {sessionToken}");
                return AuthenticateResult.Fail("Invalid token");
            }

            _logger.LogDebug($"Session authenticated for token: {sessionToken}");
            session.LastActive = DateTime.UtcNow;
            await _sessionService.UpdateSessionAsync(session);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, session.UserId.ToString()),
                new Claim("SessionID", sessionToken)
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            if (Context.Request.Path.StartsWithSegments("/api"))
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                Response.Headers["WWW-Authenticate"] = "Bearer";
            } else
            {
                Response.Redirect("/login");
            }
            
            return Task.CompletedTask;
        }

    }
}
