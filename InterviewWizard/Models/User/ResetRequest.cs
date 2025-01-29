using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InterviewWizard.Models.User
{
    public class ResetRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public Guid Token { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        [DefaultValue(false)]
        public bool Used { get; set; }
    }
}
