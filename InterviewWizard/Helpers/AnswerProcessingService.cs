using InterviewWizard.Models.Response;
using InterviewWizard.Models.Session;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OpenAI.Assistants;
using OpenAI.Threads;
using OpenAI;
using System.Threading;
using System.Runtime.CompilerServices;

namespace InterviewWizard.Helpers
{
    public class AnswerProcessingService : BackgroundService, IAnswerProcessingService
    {

        private readonly IServiceScopeFactory _scopeFactory;

        public AnswerProcessingService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken);
            }

        }

        public async Task ProcessAnswerAsync(string sessionKey, Guid questionId, string assistantId, string threadId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var _dbcontext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var _sessionService = scope.ServiceProvider.GetRequiredService<SessionService>();
                var _assistantDefinitionHelper = scope.ServiceProvider.GetRequiredService<AssistantDefinitionHelper>();

                // Use await with async methods
                Session thisSession = await _sessionService.GetSessionAsync(sessionKey);
                Question thisQuestion = thisSession.Questions.FirstOrDefault(q => q.Id == questionId);

                using OpenAIClient oac = new OpenAIClient();
                // Refactor to use await
                AssistantResponse assistant = await oac.AssistantsEndpoint.RetrieveAssistantAsync(assistantId: assistantId);
                ThreadResponse thread = await oac.ThreadsEndpoint.RetrieveThreadAsync(threadId: threadId);

                string feedbackDefinition = _assistantDefinitionHelper.GetDefinitionContent("AnswerFeedbackGenerator");
                feedbackDefinition = feedbackDefinition.Replace("{{question}}", thisQuestion.QuestionText).Replace("{{answer}}", thisQuestion.AnswerText);

                // Assume CreateMessageAsync should be awaited or its result used somehow
                await thread.CreateMessageAsync(feedbackDefinition);
                var run = await thread.CreateRunAsync(assistant);
                run = await run.WaitForStatusChangeAsync();

                // Get response
                var runResponse = await run.ListMessagesAsync();
                var answerFeedbackResponse = runResponse.Items.FirstOrDefault(m => m.Role == Role.Assistant)?.Content;
                string answerFeedbackText = answerFeedbackResponse?[0].Text.ToString();
                answerFeedbackText = answerFeedbackText.Replace("```json", "").Replace("```", "");
                AnswerFeedbackModel answerFeedback = JsonConvert.DeserializeObject<AnswerFeedbackModel>(answerFeedbackText);

                Question q = await _dbcontext.QuestionObjects.FindAsync(questionId);
                q.AnswerFeedback = answerFeedback.Feedback;
                q.AnswerRating = answerFeedback.Rating;
                await _dbcontext.SaveChangesAsync();
            }
        }


        public void FireAndForgetSafeAsync(Task task)
        {
            _ = task.ContinueWith(t =>
            {
                var exception = t.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
