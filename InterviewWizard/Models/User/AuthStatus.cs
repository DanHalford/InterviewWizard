namespace InterviewWizard.Models.User
{
    public class AuthStatus
    {
        public bool IsAuthenticated { get; set; }
        public string EmailAddress { get; set; }
        public string Token { get; set; }
    }
}
