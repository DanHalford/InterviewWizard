using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace InterviewWizard.Models.Session
{
    public class Question
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public string SessionKey { get; set; } //Foreign key

        [Required]
        [Range(1, 3)]
        public int QuestionNumber { get; set; }

        [Required]
        public string QuestionText { get; set; }

        [DefaultValue(null)]
        public string? AnswerText { get; set; }

        [DefaultValue(null)]
        public string? AnswerFeedback{ get; set; }

        [DefaultValue(null)]
        public int? AnswerRating { get; set; }

        // Navigation property
        public Session Session { get; set; }

        [DefaultValue(Rating.Unrated)]
        public Rating UserQuestionRating { get; set; }

        [DefaultValue(Rating.Unrated)]
        public Rating UserFeedbackRating { get; set; }

        public string? UserQuestionRatingText { get; set; }

        public string? UserFeedbackRatingText { get; set; }
    }
}
