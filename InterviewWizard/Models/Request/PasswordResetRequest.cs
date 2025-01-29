using System.ComponentModel.DataAnnotations;

namespace InterviewWizard.Models.Request
{
    public class PasswordResetRequest
    {
        [Required]
        public string Token { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
