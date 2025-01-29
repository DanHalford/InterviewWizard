namespace InterviewWizard.Models.Response
{
    /// <summary>
    /// Encapsulates the response model for API responses
    /// </summary>
    public class APIResponseModel
    {
        /// <summary>
        /// Optional message to return to the client
        /// </summary>
        public string? Message { get; set; }
        /// <summary>
        /// Has the API call been successful?
        /// </summary>
        public bool Success { get; set; }
    }
}
