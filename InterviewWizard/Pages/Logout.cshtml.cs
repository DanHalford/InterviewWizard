using Azure.Storage.Blobs;
using InterviewWizard.Helpers;
using InterviewWizard.Models.Session;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenAI;

namespace InterviewWizard.Pages
{
    public class LogoutModel : PageModel
    {

        private readonly SessionService _sessionService;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly ApplicationDbContext _dbcontext;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SessionService sessionService, BlobContainerClient blobContainerClient, ApplicationDbContext dbcontext, ILogger<LogoutModel> logger)
        {
            _sessionService = sessionService;
            _blobContainerClient = blobContainerClient;
            _dbcontext = dbcontext;
            _logger = logger;
        }

        public async Task<PageResult> OnGetAsync()
        {
            await DeleteSessionContents();
            return Page();
        }

        private async Task<bool> DeleteSessionContents()
        {
            _logger.BeginScope("DeleteSessionContents");
            string? sessionKey = HttpContext.Session?.GetString("SessionKey");
            _logger.LogInformation("SessionKey: {SessionKey}", sessionKey);
            if (string.IsNullOrEmpty(sessionKey))
            {
                _logger.LogInformation("SessionKey is null or empty - nothing to clear");
                return true;
            }
            Session thisSession = await _sessionService.GetSessionAsync(sessionKey);
            if (thisSession != null && thisSession.SessionObjects != null)
            {
                _logger.LogInformation("Session found - clearing session objects");
                foreach (SessionObject so in thisSession.SessionObjects)
                {
                    switch (so.ObjectType)
                    {
                        case "Resume":
                        case "Position":
                            _logger.LogInformation($"Deleting blob {so.ObjectId} of type {so.ObjectType}");
                            try
                            {
                                var blobClient = _blobContainerClient.GetBlobClient(so.ObjectId);
                                await blobClient.DeleteAsync();
                                _logger.LogInformation("Blob {BlobName} deleted", so.ObjectId);
                            } catch (Exception e)
                            {
                                _logger.LogError(e, "Error deleting blob {BlobName}", so.ObjectId);
                            }
                            
                            break;
                        case "Thread":
                            using (OpenAIClient oac = new OpenAIClient())
                            {
                                try
                                {
                                    await oac.ThreadsEndpoint.DeleteThreadAsync(so.ObjectId);
                                } catch (Exception e)
                                {
                                    _logger.LogError(e, "Error deleting thread {ThreadId}", so.ObjectId);
                                }
                                
                            }
                            break;
                        case "Assistant":
                            using (OpenAIClient oac = new OpenAIClient())
                            {
                                try
                                {
                                    await oac.AssistantsEndpoint.DeleteAssistantAsync(so.ObjectId);
                                } catch (Exception e)
                                {
                                    _logger.LogError(e, "Error deleting assistant {AssistantId}", so.ObjectId);
                                }
                            }
                            break;
                    }
                }
                try
                {
                    int count = thisSession.SessionObjects.Count;
                    _dbcontext.SessionObjects.RemoveRange(thisSession.SessionObjects);
                    _logger.LogInformation($"Removed {count} session objects from database");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error removing session objects from database");
                }
                try
                {
                    int count = thisSession.Questions.Count;
                    _dbcontext.QuestionObjects.RemoveRange(thisSession.Questions);
                    _logger.LogInformation($"Removed {count} questions from database");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error removing questions from database");
                }
                try
                {
                    await _sessionService.UpdateSessionAsync(thisSession);
                    _logger.LogInformation("Session updated.");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error removing session from database");
                }
            } else
            {
                _logger.LogInformation("Session not found - nothing to clear");
            }
            try
            {
                HttpContext.Session?.Clear();
                _logger.LogInformation("Http context session cleared");
            } catch (Exception e)
            {
                _logger.LogError(e, "Error clearing session");
            }
            return true;
        }
    }
}
