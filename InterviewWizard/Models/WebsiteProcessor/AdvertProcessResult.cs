namespace InterviewWizard.Models.WebsiteProcessor
{
    /// <summary>
    /// Used to return the result of processing an advert to the ApiController
    /// </summary>
    public class AdvertProcessResult
    {
        public bool Success { get; set; }
        public JobAdvert? Content { get; set; }
    }
}
