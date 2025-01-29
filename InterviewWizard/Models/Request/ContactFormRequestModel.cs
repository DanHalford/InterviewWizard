using System.ComponentModel.DataAnnotations;

namespace InterviewWizard.Models.Request
{
    /// <summary>
    /// Encapsulates the request model for the contact form
    /// </summary>
    public class ContactFormRequestModel
    {
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }
        public string Name { get; set; }
        [Required]
        public string Message { get; set; }
    }
}
