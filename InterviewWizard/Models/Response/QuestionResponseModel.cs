namespace InterviewWizard.Models.Response
{
    /// <summary>
    /// Response model for sending generated questions to the client
    /// </summary>
    public class QuestionResponseModel
    {
        public string Question1 { get; set; }
        public string Question2 { get; set; }
        public string Question3 { get; set; }
    }
}
