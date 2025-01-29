using InterviewWizard.Models.Session;
using Microsoft.EntityFrameworkCore;

namespace InterviewWizard.Helpers
{
    public class SessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpContext? _httpContext;

        public string SessionKey
        {
            get => _httpContext.Session.GetString("SessionKey") ?? string.Empty;
            set => _httpContext.Session.SetString("SessionKey", value);
        }
        public string ThreadId
        {
            get => _httpContext.Session.GetString("ThreadId") ?? string.Empty;
            set => _httpContext.Session.SetString("ThreadId", value);
        }
        public string AssistantId
        {
            get => _httpContext.Session.GetString("AssistantId") ?? string.Empty;
            set => _httpContext.Session.SetString("AssistantId", value);
        }
        public string ResumeId
        {
            get => _httpContext.Session.GetString("ResumeId") ?? string.Empty;
            set => _httpContext.Session.SetString("ResumeId", value);
        }

        public string PositionId
        {
            get => _httpContext.Session.GetString("PositionId") ?? string.Empty;
            set => _httpContext.Session.SetString("PositionId", value);
        }

        public SessionService(ApplicationDbContext context, IHttpContextAccessor _httpContextAccessor)
        {
            _context = context;
            _httpContext = _httpContextAccessor.HttpContext;
        }

        public async Task CreateSessionAsync(Session session)
        {
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task<Session> GetSessionAsync(string? sessionKey)
        {
            if (sessionKey == null)
            {
                throw new Exception("Session key not found in session");
            }
            return await _context.Sessions
                .Include(s => s.User)
                .Include(s => s.SessionObjects)
                .Include(s => s.Questions)
                .FirstOrDefaultAsync(s => s.SessionKey == sessionKey);
        }

        public async Task<SessionObject> GetLatestResume()
        {
            if (this.SessionKey == null)
            {
                throw new Exception("Session key not found in session");
            }
            return await _context.SessionObjects
                .Where(so => so.SessionKey == this.SessionKey && so.ObjectType == "Resume")
                .OrderByDescending(so => so.Created)
                .FirstOrDefaultAsync();
        }

        public async Task<SessionObject> GetLatestPosition()
        {
            if (this.SessionKey == null)
            {
                throw new Exception("Session key not found in session");
            }
            return await _context.SessionObjects
                .Where(so => so.SessionKey == this.SessionKey && so.ObjectType == "Position")
                .OrderByDescending(so => so.Created)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateSessionAsync(Session session)
        {
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSessionAsync(string sessionKey)
        {
            var session = await _context.Sessions.FindAsync(sessionKey);
            if (session != null)
            {
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ListSessions()
        {
            await _context.Sessions.ToListAsync();
        }

        public async Task<List<SessionObject>> GetSessionObjects(string sessionKey)
        {
            return await _context.SessionObjects.Where(so => so.SessionKey == sessionKey).ToListAsync();
        }

        public async Task CreateSessionObjectAsync(Session session, SessionObject sessionObject)
        {
            session.SessionObjects.Add(sessionObject);
            await _context.SaveChangesAsync();
        }
    }
}
