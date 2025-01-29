namespace InterviewWizard.Models.DocumentProcessing
{

    public enum DocumentType
    {
        Resume,
        Position,
        Question,
        Answer
    }
    public static class DocumentTypeExtensions
    {
        public static string ToString(this DocumentType documentType)
        {
            return documentType.ToString();
        }
    }
}
