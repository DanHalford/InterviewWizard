namespace InterviewWizard.Models.WebsiteProcessor
{
   /// <summary>
   /// Encapsulates a job advert as a data structure
   /// </summary>
    public class JobAdvert
    {
        public Uri SourceUrl { get; set; }
        public string JobTitle { get; set; }
        public string Advertiser { get; set; }
        public string Details { get; set; }
    }
}
