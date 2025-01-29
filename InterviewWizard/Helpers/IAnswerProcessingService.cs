namespace InterviewWizard.Helpers
{
    public interface IAnswerProcessingService
    {
        Task ProcessAnswerAsync(string sessionKey, Guid questionId, string assistantId, string threadId);
    }
}
