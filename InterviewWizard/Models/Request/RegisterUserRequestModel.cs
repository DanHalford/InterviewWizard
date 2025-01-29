using System.ComponentModel.DataAnnotations;

namespace InterviewWizard.Models.Request
{
    /// <summary>
    /// Encapsulates the request model for registering a user
    /// </summary>
    public class RegisterUserRequestModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string token { get; set; }
    }

}
