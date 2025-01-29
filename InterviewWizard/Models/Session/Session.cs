namespace InterviewWizard.Models.Session
{
    public class Session
    {
        public string SessionKey { get; set; } //Primary Key
        public DateTime LastActive { get; set; }

        public Guid? UserId { get; set; } // Nullable foreign key

        public InterviewWizard.Models.User.User User { get; set; } //Foreign Key

        //Navigation Property
        public List<SessionObject> SessionObjects { get; set; } = new List<SessionObject>();

        public List<Question> Questions { get; set; } = new List<Question>();

        public string Epilogue { get; set; }

    }
}
