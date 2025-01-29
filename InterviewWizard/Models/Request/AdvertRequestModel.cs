using System.ComponentModel.DataAnnotations;

namespace InterviewWizard.Models.Request
{
    /// <summary>
    /// Encapsulates passing a URL to the API Controller for job advert analysis
    /// </summary>
    public class AdvertRequestModel
    {
        [Required]
        public string Url { get; set; }
    }
}
