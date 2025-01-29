using System.ComponentModel.DataAnnotations;

namespace InterviewWizard.Models.Request
{
    /// <summary>
    /// Encapsulates the answers provided by the candidate for submission to the API Controller
    /// </summary>
    public class AnswerRequestModel
    {
        [Required]
        [Range(1, 3)]
        public int QuestionId { get; set; }
        [Required]
        public string Answer { get; set; }
    }
}
