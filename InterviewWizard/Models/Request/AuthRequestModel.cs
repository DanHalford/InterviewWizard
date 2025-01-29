using System.ComponentModel.DataAnnotations;

namespace InterviewWizard.Models.Request
{
    /// <summary>
    /// Encapsulates the request model for authentication
    /// </summary>
    public class AuthRequestModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        public string Password { get; set; }
    }

}
