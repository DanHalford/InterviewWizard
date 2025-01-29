namespace InterviewWizard.Models.Response
{
    /// <summary>
    /// Encapsulates the response model for authentication
    /// </summary>
    public class AuthResponseModel
    {
        public string? Token { get; set; }
        public string? Message { get; set; }
        public bool Success { get; set; }
    }
}
