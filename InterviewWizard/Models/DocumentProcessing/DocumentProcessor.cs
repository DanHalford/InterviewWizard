using System.IO;
using System.Text;
using DocumentFormat.OpenXml.Packaging;

namespace InterviewWizard.Models.DocumentProcessing
{
    public class DocumentProcessor
    {
        public string ProcessDocument(Stream fileContent)
        {
            StringBuilder sb = new StringBuilder();
            using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(fileContent, false))
            {
                var body = wordDocument.MainDocumentPart.Document.Body;
                foreach (var paragraph in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                {
                    sb.AppendLine(paragraph.InnerText);
                }
            }
            return sb.ToString();
        }
    }
}
