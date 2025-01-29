namespace InterviewWizard.Models.Storage
{
    /// <summary>
    /// Encapsulates a blob text file as a data structure
    /// </summary>
    public class BlobFile
    {
        public string? Filename { get; set; }
        public string? Content { get; set; }
        public DateTime? DateUploaded { get; set; } = DateTime.UtcNow;
        public string? Url { get; set; }
    }
}
