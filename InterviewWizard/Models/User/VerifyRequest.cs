using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterviewWizard.Models.User
{
    public class VerifyRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Foreign key for User table
        public Guid UserId { get; set; }

        [Required]
        public User User { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        [DefaultValue(false)]
        public bool Used { get; set; }
    }
}
