namespace InterviewWizard.Models.Response
{
    public class InterviewPlanSectionModel
    {
        public string SectionTitle { get; set; }
        public int SuggestedDuration { get; set; }
        public List<string> Actions { get; set; }
        public string ExampleScript { get; set; }
    }
}
