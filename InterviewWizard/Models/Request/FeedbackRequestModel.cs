using System.ComponentModel.DataAnnotations;

namespace InterviewWizard.Models.Request
{
    public class FeedbackRequestModel
    {
        [Required]
        public string QuestionId { get; set;}

        [Required]
        public int Rating { get; set; }

        public string? FeedbackText { get; set; }
    }
}