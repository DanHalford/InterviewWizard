using Azure.Storage.Blobs;
using InterviewWizard.Models.Storage;
using InterviewWizard.Models.DocumentProcessing;
using InterviewWizard.Models.WebsiteProcessor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Threads;
using OpenAI.Assistants;
using InterviewWizard.Models.Session;
using Microsoft.Graph;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using InterviewWizard.Models.Utility;
using InterviewWizard.Models.Request;
using InterviewWizard.Models.Response;
using InterviewWizard.Helpers;
using Newtonsoft.Json;
using OpenAI.Files;
using DocumentFormat.OpenXml.Office2010.Excel;
using InterviewWizard.Models.User;

namespace InterviewWizard.Controllers
{
    [Authorize(AuthenticationSchemes = SessionTokenAuthenticationHandler.SchemeName)]
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _blobContainerClient;
        private readonly AppSettings _appSettings;
        private readonly ApplicationDbContext _dbcontext;
        private readonly SessionService _sessionService;
        private readonly GraphServiceClient _graphClient;
        private readonly AssistantDefinitionHelper _assistantDefinitionHelper;
        private readonly ILogger<ApiController> _logger;
        private readonly IAnswerProcessingService _answerProcessingService;

        public ApiController(
            BlobServiceClient blobServiceClient, 
            BlobContainerClient blobContainerClient, 
            IOptions<AppSettings> appSettings, 
            ApplicationDbContext dbcontext, 
            SessionService sessionService,
            GraphServiceClient graphServiceClient,
            AssistantDefinitionHelper assistantDefinitionHelper,
            ILogger<ApiController> logger,
            IAnswerProcessingService answerProcessingService)

        {
            _blobServiceClient = blobServiceClient;
            _blobContainerClient = blobContainerClient;
            _appSettings = appSettings.Value;
            _dbcontext = dbcontext;
            _sessionService = sessionService;
            _graphClient = graphServiceClient;
            _assistantDefinitionHelper = assistantDefinitionHelper;
            _logger = logger;
            _answerProcessingService = answerProcessingService;
        }

        [HttpGet]
        [Route("sendTestEmail")]
        public async Task<IActionResult> SendTestEmail()
        {
            var user = HttpContext.User;
            var userId = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var sessionId = user.Claims.FirstOrDefault(c => c.Type == "SessionID")?.Value;

            EmailService emailService = new EmailService(_graphClient, _appSettings);
            await emailService.SendEmailAsync("dan@sanx.org", "Test IW email", $"User Id: {userId}<br />Session Id: {sessionId}");
            return Ok();
        }

        [HttpDelete]
        [Route("endSession")]
        public async Task<IActionResult> EndSession()
        {
            if (string.IsNullOrWhiteSpace(_sessionService.SessionKey) ==false)
            {
                await DeleteSessionObjects();
            }
            return NoContent();
        }

        [HttpPost]
        [Route("uploadFile/{type}")]
        public async Task<IActionResult> UploadFile(IFormFile file, [FromRoute]string type)
        {
            using(_logger.BeginScope("UploadFile"))
            {
                string fileContent;
                try
                {
                    fileContent = ProcessDocument(file);
                    _logger.LogInformation("File content processed");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error processing file content: {e.Message}");
                    return BadRequest(e.Message);
                }
                DocumentType docType;
                switch (type) {
                    case "resume":
                        docType = DocumentType.Resume;
                        break;
                    case "position":
                        docType = DocumentType.Position;
                        break;
                    default:
                        return BadRequest("INVALIDTYPE");
                }
                try
                {
                    string filename = await UploadContent(fileContent, docType);
                    _logger.LogInformation($"File content uploaded with file ID: {filename}");
                    return Created("", filename);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error uploading file content: {e.Message}");
                    return BadRequest(e.Message);
                }
            }
        }

        private string ProcessDocument(IFormFile file)
        {
            var contentType = file.ContentType;
            if (contentType != "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                throw new Exception("INVALIDDOCUTMENTTYPE");
            }

            var dp = new DocumentProcessor();
            string fileContent = dp.ProcessDocument(file.OpenReadStream());
            return fileContent;
        }

        private async Task<string> UploadContent(string content, DocumentType type)
        {
            using (_logger.BeginScope("UploadContent"))
            {
                Blob blob = new Blob(_blobContainerClient);
                BlobFile blobFile = new BlobFile()
                {
                    Content = content
                };
                blobFile = await blob.SaveBlob(blobFile);
                if (blobFile == null)
                {
                    _logger.LogError("Error saving file content");
                    throw new Exception("SAVEFILEERROR");
                }
                switch (type)
                {
                    case DocumentType.Resume:
                        _sessionService.ResumeId = blobFile.Filename;
                        _logger.LogInformation($"Resume ID set to {_sessionService.ResumeId}");
                        break;
                    case DocumentType.Position:
                        _sessionService.PositionId = blobFile.Filename;
                        _logger.LogInformation($"Position ID set to {_sessionService.PositionId}");
                        break;
                    default:
                        throw new NotImplementedException();
                }
                Session thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);
                await _sessionService.UpdateSessionAsync(thisSession);
                SessionObject thisFile = SessionFactory.CreateSessionObject(thisSession.SessionKey, blobFile.Filename, type.ToString());
                _logger.LogInformation($"Session object of type {type} created with ID: {thisFile.ObjectId}");
                thisSession.SessionObjects.Add(thisFile);
                await _dbcontext.SaveChangesAsync();
                return blobFile.Filename;
            }
        }

        [HttpPost]
        [Route("upload/{type}")]
        public async Task<IActionResult> Upload([FromRoute]string type, [FromBody]ContentRequestModel content)
        {
            if (content == null || string.IsNullOrWhiteSpace(content.Content))
            {
                return BadRequest("MISSINGCONTENT");
            }
            switch(type)
            {
                case "resume":
                    var personDetails = await AnalysePastedCV(content.Content);
                    await UploadContent(content.Content, DocumentType.Resume);
                    return Ok(personDetails);
                case "position":
                    var positionDetails = await AnalysePastedPositionDescription(content.Content);
                    await UploadContent(content.Content, DocumentType.Position);
                    return Ok(positionDetails);
                    default:
                    return BadRequest("INVALIDTYPE");
            }
        }

        private async Task<JobAdvert> AnalysePastedPositionDescription(string content)
        {
            using OpenAIClient oac = new OpenAIClient();
            string assistantDefinition = _assistantDefinitionHelper.GetDefinitionContent("PositionDescriptionAnalyser");
            var request = new CreateAssistantRequest(
                _appSettings.OpenAI.ComplexModelName,   //Model
                $"InterviewWizard-{_sessionService.SessionKey}", //Name
                null, //Description
                assistantDefinition);
            AssistantResponse assistant = await oac.AssistantsEndpoint.CreateAssistantAsync(request);
            ThreadResponse thread = await oac.ThreadsEndpoint.CreateThreadAsync();
            await thread.CreateMessageAsync($"This is the position description: {content}");
            await thread.CreateMessageAsync("Please analyse for job title and employer.");
            var run = await thread.CreateRunAsync(assistant);
            run = await run.WaitForStatusChangeAsync();
            var runResponse = await run.ListMessagesAsync();
            var positionDetails = runResponse.Items.Where(m => m.Role == Role.Assistant).Select(m => m.Content).ToList().FirstOrDefault();
            JobAdvert ja = new JobAdvert();
            if (positionDetails != null && string.IsNullOrWhiteSpace( positionDetails[0].Text.ToString()) == false)
            {
                var resultLines = positionDetails[0].Text.ToString()!.Split("\n");
                if (resultLines.Length == 2 && resultLines[0].StartsWith("title:") && resultLines[1].StartsWith("employer:"))
                {
                    var title = resultLines[0].Substring("title:".Length).Trim();
                    var employer = resultLines[1].Substring("employer:".Length).Trim();
                    ja = new JobAdvert()
                    {
                        JobTitle = title,
                        Advertiser = employer
                    };
                }
            }
            await oac.ThreadsEndpoint.DeleteThreadAsync(thread.Id);
            await oac.AssistantsEndpoint.DeleteAssistantAsync(assistant.Id);
            return ja;
        }

        private async Task<CandidateResponseModel> AnalysePastedCV(string content)
        {
            using OpenAIClient oac = new OpenAIClient();
            string assistantDefinition = _assistantDefinitionHelper.GetDefinitionContent("ResumeAnalyser");
            var request = new CreateAssistantRequest(
                _appSettings.OpenAI.ComplexModelName,   //Model
                $"InterviewWizard-{_sessionService.SessionKey}", //Name
                null, //Description
                assistantDefinition);
            AssistantResponse assistant = await oac.AssistantsEndpoint.CreateAssistantAsync(request);
            ThreadResponse thread = await oac.ThreadsEndpoint.CreateThreadAsync();
            await thread.CreateMessageAsync($"This is the position description: {content}");
            await thread.CreateMessageAsync("Please analyse for job title and employer.");
            var run = await thread.CreateRunAsync(assistant);
            run = await run.WaitForStatusChangeAsync();
            var runResponse = await run.ListMessagesAsync();
            var positionDetails = runResponse.Items.Where(m => m.Role == Role.Assistant).Select(m => m.Content).ToList().FirstOrDefault();
            CandidateResponseModel cm = new CandidateResponseModel();
            if (positionDetails != null && string.IsNullOrWhiteSpace(positionDetails[0].Text.ToString()) == false)
            {
                var resultLines = positionDetails[0].Text.ToString()!.Split("\n");
                if (resultLines.Length == 2 && resultLines[0].StartsWith("name:") && resultLines[1].StartsWith("title:"))
                {
                    var name = resultLines[0].Substring("name:".Length).Trim();
                    var title = resultLines[1].Substring("title:".Length).Trim();
                    cm = new CandidateResponseModel()
                    {
                        Name = name,
                        Title = title
                    };
                }
            }
            await oac.ThreadsEndpoint.DeleteThreadAsync(thread.Id);
            await oac.AssistantsEndpoint.DeleteAssistantAsync(assistant.Id);
            return cm;
        }

        [HttpGet]
        [Route("list")]
        public async Task<IActionResult> ListFiles()
        {
            Blob blob = new Blob(_blobContainerClient);
            var files = await blob.ListFiles();
            return Ok(files);
        }

        [HttpGet]
        [Route("sessionInfo")]
        public IActionResult GetSessionInfo()
        {
            return Ok(new { sessionKey = _sessionService.SessionKey });
        }

        [HttpPost]
        [Route("advert")]
        public async Task<IActionResult> GetAdvert([FromBody] AdvertRequestModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
            {
                return BadRequest("MISSINGURL");
            }
            Uri uri = new Uri("about:blank");
            try
            {
               uri = new Uri(request.Url);
            }
            catch (Exception e)
            {
                return BadRequest("INVALIDURL");
            }
            
            var host = uri.Host;
            AdvertProcessor ap = new AdvertProcessor();
            AdvertProcessResult result = ap.ProcessUrl(request.Url).Result;
            if (result.Success == false)
            {
                return BadRequest("INVALIDADVERTSOURCE");
            }
            string content = Newtonsoft.Json.JsonConvert.SerializeObject(result.Content);

            
            try
            {
                var filename = await UploadContent(content, DocumentType.Position);
                return Created("", result);
            } catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [Route("plan")]
        public async Task<IActionResult> GetInterviewPlan()
        {
            throw new NotImplementedException();

            Session thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);

            //Consume credit
            User? thisUser = await _dbcontext.Users.FindAsync(thisSession.UserId);
            if (thisUser.SubscriptionType == SubscriptionType.Free && thisUser.Credits < 1)
            {
                return BadRequest("INSUFFICIENTCREDITS");
            }
            thisUser.Credits = thisUser.Credits - 2;
            _dbcontext.Users.Update(thisUser);

            Blob blob = new Blob(_blobContainerClient);
            SessionObject? resumeObject = await _sessionService.GetLatestResume();
            SessionObject? positionObject = await _sessionService.GetLatestPosition();
            BlobFile? resume = await blob.GetBlob(resumeObject.ObjectId);
            BlobFile? position = await blob.GetBlob(positionObject.ObjectId);
            if (resume == null || position == null)
            {
                return BadRequest("MISSINGRESUMEORPOSITION");
            }
            var sessionTag = new Dictionary<string, string>
            {
                { "sessionKey", _sessionService.SessionKey }
            };

            using OpenAIClient oac = new OpenAIClient();
            string assistantDefinition = _assistantDefinitionHelper.GetDefinitionContent("InterviewPlan");
            var request = new CreateAssistantRequest(
                _appSettings.OpenAI.ComplexModelName,
                $"InterviewWizard-{_sessionService.SessionKey}",
                null, //Description,
                assistantDefinition,
                null,
                null
            );

            
            AssistantResponse assistant = new AssistantResponse();
            try
            {
                assistant = await oac.AssistantsEndpoint.CreateAssistantAsync(request);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating assistant");
                return StatusCode(503, "CREATEASSISTANTERROR");
            }
            _sessionService.AssistantId = assistant.Id;

            var thread = await oac.ThreadsEndpoint.CreateThreadAsync();
            _sessionService.ThreadId = thread.Id;


            SessionObject thisThread = SessionFactory.CreateSessionObject(thisSession.SessionKey, thread.Id, "Thread");
            SessionObject thisAssistant = SessionFactory.CreateSessionObject(thisSession.SessionKey, assistant.Id, "Assistant");
            await _sessionService.UpdateSessionAsync(thisSession);
            await _sessionService.CreateSessionObjectAsync(thisSession, thisThread);
            await _sessionService.CreateSessionObjectAsync(thisSession, thisAssistant);

            //Tag thread with session key
            thread = await thread.ModifyAsync(sessionTag);

            //Create initial user messages to send Resume and Position to assistant
            await thread.CreateMessageAsync($"This is the resume:\n{resume.Content}\n\nThis is the position description:\n{position.Content}");
            await thread.CreateMessageAsync("Generate my interview plan.");

            //Run thread and wait for it to complete
            var run = await thread.CreateRunAsync(assistant);
            run = await run.WaitForStatusChangeAsync();

            //Get response messages
            var runResponse = await run.ListMessagesAsync();
            var planResponse = runResponse.Items.Where(m => m.Role == Role.Assistant).Select(m => m.Content).ToList().FirstOrDefault();
            if (planResponse == null)
            {
                throw new Exception("NOPLANRESPONSE");
            }
            string planText = planResponse[0].Text.ToString()!;
            planText = planText.Replace("```json", "").Replace("```", "");
            var planWrapper = JsonConvert.DeserializeObject<InterviewPlanModel>(planText);

            if (planWrapper == null)
            {
                return BadRequest("INVALIDPLANRESPONSE");
            }
        }

        [HttpGet]
        [Route("questions")]
        public async Task<IActionResult> GetQuestions()
        {
            Session thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);

            //Consume credit
            User? thisUser = await _dbcontext.Users.FindAsync(thisSession.UserId);
            if (thisUser.SubscriptionType == SubscriptionType.Free && thisUser.Credits < 1)
            {
                return BadRequest("INSUFFICIENTCREDITS");
            }
            thisUser.Credits--;
            _dbcontext.Users.Update(thisUser);
            
            Blob blob = new Blob(_blobContainerClient);
            SessionObject? resumeObject = await _sessionService.GetLatestResume();
            SessionObject? positionObject = await _sessionService.GetLatestPosition();
            BlobFile? resume = await blob.GetBlob(resumeObject.ObjectId);
            BlobFile? position = await blob.GetBlob(positionObject.ObjectId);
            if (resume == null || position == null)
            {
                return BadRequest("MISSINGRESUMEORPOSITION");
            }
            var sessionTag = new Dictionary<string, string>
            {
                { "sessionKey", _sessionService.SessionKey }
            };

            using OpenAIClient oac = new OpenAIClient();
            List<Tool> tools = new List<Tool>();
            List<string> files = new List<string>();
            tools.Add(Tool.FileSearch);
            
            if (_appSettings.KnowledgeFiles != null)
            {
                foreach (string file in _appSettings.KnowledgeFiles)
                {
                    FileResponse f = await oac.FilesEndpoint.GetFileInfoAsync(file);
                    if (f == null)
                    {
                        throw new Exception("MISSINGKNOWLEDGEFILE");
                    }
                    files.Add(file);
                }
            }

            string assistantDefinition = _assistantDefinitionHelper.GetDefinitionContent("QuestionGenerator");
            AssistantResponse assistant = new AssistantResponse();
            var request = new CreateAssistantRequest(
                assistant,
                _appSettings.OpenAI.ComplexModelName,
                $"InterviewWizard-{_sessionService.SessionKey}",
                null, //Description
                assistantDefinition,
                tools,
                files,
                null);
            
            try
            {
                assistant = await oac.AssistantsEndpoint.CreateAssistantAsync(request);
            } catch (Exception e)
            {
                _logger.LogError(e, "Error creating assistant");
                return StatusCode(503, "CREATEASSISTANTERROR");
            }
            _sessionService.AssistantId = assistant.Id;

            var thread = await oac.ThreadsEndpoint.CreateThreadAsync();
            _sessionService.ThreadId = thread.Id;

            
            SessionObject thisThread = SessionFactory.CreateSessionObject(thisSession.SessionKey, thread.Id, "Thread");
            SessionObject thisAssistant = SessionFactory.CreateSessionObject(thisSession.SessionKey, assistant.Id, "Assistant");
            await _sessionService.UpdateSessionAsync(thisSession);
            await _sessionService.CreateSessionObjectAsync(thisSession, thisThread);
            await _sessionService.CreateSessionObjectAsync(thisSession, thisAssistant);

            //Tag thread with session key
            thread = await thread.ModifyAsync(sessionTag);

            //Create initial user messages to send Resume and Position to assistant
            await thread.CreateMessageAsync($"This is my resume:\n{resume.Content}\n\nThis is the position description:\n{position.Content}");
            await thread.CreateMessageAsync("Generate my sample interview questions.");

            //Run thread and wait for it to complete
            var run = await thread.CreateRunAsync(assistant);
            run = await run.WaitForStatusChangeAsync();

            //Get response messages
            var runResponse = await run.ListMessagesAsync();
            var questionResponse = runResponse.Items.Where(m => m.Role == Role.Assistant).Select(m => m.Content).ToList().FirstOrDefault();
            if (questionResponse == null)
            {
                throw new Exception("NOQUESTIONRESPONSE");
            }
            string questionText = questionResponse[0].Text.ToString()!;
            questionText = questionText.Replace("```json", "").Replace("```", "");
            var questionsWrapper = JsonConvert.DeserializeObject<QuestionsWrapperModel>(questionText);

            if (questionsWrapper == null || questionsWrapper.Questions.Count != 3)
            {
                return BadRequest("INVALIDQUESTIONRESPONSE");
            }

            foreach (QuestionModel qm in questionsWrapper.Questions)
            {
                Question q = SessionFactory.CreateQuestion(thisSession, qm);
                await _dbcontext.QuestionObjects.AddAsync(q);
            }
            await _sessionService.UpdateSessionAsync(thisSession);

            return Ok();
        }

        [HttpPost]
        [Route("questions/{id}/answer")]
        public async Task<IActionResult> SubmitAnswer([FromRoute]int id, [FromBody]AnswerRequestModel answer)
        {
            if (id < 1 || id > 3)
            {
                return BadRequest("INVALIDQUESTIONID");
            }   
            if (answer == null || string.IsNullOrWhiteSpace(answer.Answer))
            {
                return BadRequest("MISSINGANSWER");
            }
            AnswerRequestModel answerObject = new AnswerRequestModel()
            {
                QuestionId = id,
                Answer = answer.Answer
            };

            Session thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);
            if (thisSession == null)
            {
                return BadRequest("SESSIONNOTFOUND");
            }
            Question thisQuestion = thisSession.Questions.Where(q => q.QuestionNumber == id).FirstOrDefault();
            if (thisQuestion == null)
            {
                return BadRequest("QUESTIONNOTFOUND");
            }
            thisQuestion.AnswerText = answer.Answer;
            await _sessionService.UpdateSessionAsync(thisSession);
            if (id == 3)
            {
                await GetAnswerFeedback(thisSession.SessionKey, thisQuestion.Id, _sessionService.AssistantId, _sessionService.ThreadId);
            } else
            {
                TaskUtilities.FireAndForgetSafeAsync(
                    _answerProcessingService.ProcessAnswerAsync(
                        thisSession.SessionKey,
                        thisQuestion.Id,
                        _sessionService.AssistantId,
                        _sessionService.ThreadId
                ));
            }
            return Ok();
        }

        [HttpGet]
        [Route("prepareFeedback")]
        public async Task<IActionResult> PrepareFeedback()
        {
            Session thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);
            Question? thisQuestion = thisSession.Questions.Where(q => q.QuestionNumber == 3).FirstOrDefault();
            if (thisQuestion == null)
            {
                return BadRequest("QUESTIONSNOTCOMPLETE");
            }
            await GetAnswerFeedback(thisSession.SessionKey, thisQuestion.Id, _sessionService.AssistantId, _sessionService.ThreadId);
            await GetEpilogue(thisSession.SessionKey, thisQuestion.Id, _sessionService.AssistantId, _sessionService.ThreadId);
            return Ok();
        }

        [HttpPost]
        [Route("feedback/{id}")]
        public async Task<IActionResult> SubmitFeedback([FromRoute]string id, [FromBody]FeedbackRequestModel feedback)
        {
            if (feedback == null || feedback.QuestionId == null)
            {
                return BadRequest("MISSINGFEEDBACK");
            }
            Guid QuestionId = Guid.NewGuid();
            try
            {
                QuestionId = Guid.Parse(id);
            } catch {
                return BadRequest("INVALIDQUESTIONID");
            }
            Session thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);
            if (thisSession == null)
            {
                return BadRequest("SESSIONNOTFOUND");
            }
            Question? thisQuestion = thisSession.Questions.FirstOrDefault(q => q.Id == QuestionId);
            if (thisQuestion == null)
            {
                return BadRequest("QUESTIONNOTFOUND");
            }
            thisQuestion.UserFeedbackRating = (Rating)feedback.Rating;
            thisQuestion.UserFeedbackRatingText = feedback.FeedbackText;
            await _sessionService.UpdateSessionAsync(thisSession);
            return Ok();
        }

        private async Task<bool> GetAnswerFeedback(string sessionKey, Guid questionId, string assistantId, string threadId)
        {
            Session thisSession = await _sessionService.GetSessionAsync(sessionKey);
            Question? thisQuestion = thisSession.Questions.Where(q => q.Id == questionId).FirstOrDefault();
            if (thisQuestion == null)
            {
                throw new Exception("QUESTIONNOTFOUND");
            }
            using OpenAIClient oac = new OpenAIClient();
            AssistantResponse assistant = await oac.AssistantsEndpoint.RetrieveAssistantAsync(assistantId: assistantId);
            ThreadResponse thread = await oac.ThreadsEndpoint.RetrieveThreadAsync(threadId: threadId);
            string feedbackDefinition = _assistantDefinitionHelper.GetDefinitionContent("AnswerFeedbackGenerator");
            feedbackDefinition = feedbackDefinition.Replace("{{question}}", thisQuestion.QuestionText).Replace("{{answer}}", thisQuestion.AnswerText);
            
            await thread.CreateMessageAsync(feedbackDefinition);
            var run = await thread.CreateRunAsync(assistant);
            run = await run.WaitForStatusChangeAsync();

            //Get response
            var runResponse = await run.ListMessagesAsync();
            var answerFeedbackResponse = runResponse.Items.Where(m => m.Role == Role.Assistant).Select(m => m.Content).ToList().FirstOrDefault();
            if (answerFeedbackResponse == null || answerFeedbackResponse[0].Text == null)
            {
                throw new Exception("NOANSWERFEEDBACK");
            }
            string answerFeedbackText = answerFeedbackResponse[0]!.Text.ToString()!;
            
            answerFeedbackText = answerFeedbackText.Replace("```json", "").Replace("```", "");
            AnswerFeedbackModel answerFeedback = JsonConvert.DeserializeObject<AnswerFeedbackModel>(answerFeedbackText);

            Question q = await _dbcontext.QuestionObjects.FindAsync(questionId);
            q.AnswerFeedback = answerFeedback.Feedback;
            q.AnswerRating = answerFeedback.Rating;
            await _dbcontext.SaveChangesAsync();
            return true;
        }

        private async Task<bool> GetEpilogue(string sessionKey, Guid questionId, string assistantId, string threadId)
        {
            Session thisSession = await _sessionService.GetSessionAsync(sessionKey);
            using OpenAIClient oac = new OpenAIClient();
            AssistantResponse assistant = await oac.AssistantsEndpoint.RetrieveAssistantAsync(assistantId: assistantId);
            ThreadResponse thread = await oac.ThreadsEndpoint.RetrieveThreadAsync(threadId: threadId);
            string epilogueDefinition = _assistantDefinitionHelper.GetDefinitionContent("EpilogueGenerator");

            await thread.CreateMessageAsync(epilogueDefinition);
            var run = await thread.CreateRunAsync(assistant);
            run = await run.WaitForStatusChangeAsync();

            //Get response
            var runResponse = await run.ListMessagesAsync();
            var epilogueResponse = runResponse.Items.Where(m => m.Role == Role.Assistant).Select(m => m.Content).ToList().FirstOrDefault();
            if (epilogueResponse == null || epilogueResponse[0].Text == null)
            {
                throw new Exception("NOEPILOGUE");
            }
            string epilogueText = epilogueResponse[0].Text.ToString()!;
            epilogueText = epilogueText.Replace("```json", "").Replace("```", "");
            EpilogueModel epilogue = JsonConvert.DeserializeObject<EpilogueModel>(epilogueText);

            thisSession.Epilogue = epilogue.Feedback;
            await _dbcontext.SaveChangesAsync();
            return true;
        }

        [HttpPost]
        [Route("reset")]
        public async Task<IActionResult> ResetSession()
        {
            if (string.IsNullOrWhiteSpace(_sessionService.SessionKey) == false)
            {
                await DeleteSessionObjects();
            }
            return Ok();
        }

        [HttpPost]
        [Route("updateEmail")]
        public async Task<IActionResult> UpdateEmail([FromBody] AuthRequestModel creds)
        {
            if (creds == null || string.IsNullOrWhiteSpace(creds.Email))
            {
                return BadRequest("INVALIDEMAIL");
            }
            Session? thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);
            User? thisUser = await _dbcontext.Users.FindAsync(thisSession.UserId);
            if (thisUser == null)
            {
                return BadRequest("USERNOTFOUND");
            } else 
            {
                thisUser.Email = creds.Email;
                await _dbcontext.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        [Route("updatePassword")]
        public async Task<IActionResult> UpdatePasswsord([FromBody] AuthRequestModel creds)
        {
            if (creds == null || string.IsNullOrWhiteSpace(creds.Password))
            {
                return BadRequest("INVALIDPASSWORD");
            }
            Session? thisSession = await _sessionService.GetSessionAsync(_sessionService.SessionKey);
            User? thisUser = await _dbcontext.Users.FindAsync(thisSession.UserId);
            if (thisUser == null)
            {
                return BadRequest("USERNOTFOUND");
            }
            else
            {
                var salt = Models.User.User.CreateSalt();
                thisUser.Salt = salt;
                thisUser.Password = await Models.User.User.CreatePasswordHash(creds.Password, salt);
                await _dbcontext.SaveChangesAsync();
            }
            return Ok();
        }

        private async Task<bool> DeleteSessionObjects()
        {
            using (_logger.BeginScope("DeleteSessionObjects"))
            {
                List<SessionObject> sessionObjects = await _sessionService.GetSessionObjects(_sessionService.SessionKey);
                foreach (SessionObject so in sessionObjects)
                {
                    switch (so.ObjectType)
                    {
                        case "Resume":
                        case "Position":
                            var blobClient = _blobContainerClient.GetBlobClient(so.ObjectId);
                            try
                            {
                                _logger.LogInformation($"Deleting blob {so.ObjectId}");
                                await blobClient.DeleteAsync();
                            }
                            catch {
                                _logger.LogWarning($"Error deleting blob {so.ObjectId}");
                            }
                            break;
                        case "Thread":
                            using (OpenAIClient oac = new OpenAIClient())
                            {
                                var thread = await oac.ThreadsEndpoint.RetrieveThreadAsync(so.ObjectId);
                                try
                                {
                                    _logger.LogInformation($"Deleting thread {so.ObjectId}");
                                    await thread.DeleteAsync();
                                }
                                catch {
                                    _logger.LogWarning($"Error deleting thread {so.ObjectId}");
                                }
                            }
                            break;
                        case "Assistant":
                            using (OpenAIClient oac = new OpenAIClient())
                            {
                                var assistant = await oac.AssistantsEndpoint.RetrieveAssistantAsync(so.ObjectId);
                                try
                                {
                                    _logger.LogInformation($"Deleting assistant {so.ObjectId}");
                                    await assistant.DeleteAsync();
                                }
                                catch {
                                    _logger.LogWarning($"Error deleting assistant {so.ObjectId}");
                                }
                            }
                            break;
                    }
                }
                _dbcontext.SessionObjects.RemoveRange(sessionObjects);
                await _dbcontext.SaveChangesAsync();
                return true;
            }
        }

        private async Task<byte[]> GetSpeechToText(string text)
        {
            using OpenAIClient oac = new OpenAIClient();
            var request = new OpenAI.Audio.SpeechRequest(text, model: "tts-1-hd", voice: OpenAI.Audio.SpeechVoice.Fable, OpenAI.Audio.SpeechResponseFormat.MP3);
            ReadOnlyMemory<byte> response = await oac.AudioEndpoint.CreateSpeechAsync(request);
            return response.ToArray();
        }

        
    }
}
