namespace InterviewWizard.Models.Response
{
    public class InterviewPlanModel
    {
        public List<InterviewPlanSectionModel> Sections { get; set; }
        public List<QuestionModel> Questions { get; set; }
    }
}
