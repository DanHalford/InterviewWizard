using System.ComponentModel;

namespace InterviewWizard.Models.Session
{
    public class SessionObject
    {
        public int Id { get; set; } // Primary Key
        public string SessionKey { get; set; } // Foreign Key
        public string ObjectId { get; set; }
        public string ObjectType { get; set; } // To distinguish between files, threads, messages, etc.

        public DateTime Created { get; set; }

        // Navigation property
        public Session Session { get; set; }
    }
}
