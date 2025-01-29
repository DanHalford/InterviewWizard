using InterviewWizard.Models.Response;
using InterviewWizard.Models.Session;

namespace InterviewWizard.Helpers
{
    public class SessionFactory
    {
        public static Session CreateSession(string sessionKey)
        {
            return new Session
            {
                SessionKey = sessionKey,
                LastActive = DateTime.UtcNow,
                Epilogue = ""
            };
        }

        public static SessionObject CreateSessionObject(string sessionKey, string objectId, string objectType)
        {
            return new SessionObject
            {
                SessionKey = sessionKey,
                ObjectId = objectId,
                ObjectType = objectType,
                Created = DateTime.UtcNow
            };
        }

        public static SessionObject CreateSessionObject(Session session, string objectId, string objectType)
        {
            return new SessionObject
            {
                SessionKey = session.SessionKey,
                ObjectId = objectId,
                ObjectType = objectType,
                Created = DateTime.UtcNow
            };
        }

        public async static void UpdateSession(SessionService sessionService, string sessionKey)
        {
            Session s = sessionService.GetSessionAsync(sessionKey).Result;
            if (s != null)
            {
                s.LastActive = DateTime.UtcNow;
                await sessionService.UpdateSessionAsync(s);
            }
        }

        public static Question CreateQuestion(string sessionKey, QuestionModel question)
        {
            return new Question
            {
                SessionKey = sessionKey,
                QuestionText = question.Question,
                QuestionNumber = question.Number
            };
        }

        public static Question CreateQuestion(Session session, QuestionModel question)
        {
            return new Question
            {
                SessionKey = session.SessionKey,
                QuestionText = question.Question,
                QuestionNumber = question.Number
            };
        }

        public static Question CreateQuestion(string sessionKey, string questionText, int questionNumber)
        {
            return new Question
            {
                SessionKey = sessionKey,
                QuestionText = questionText,
                QuestionNumber = questionNumber
            };
        }

        public static Question CreateQuestion(Session session, string questionText, int questionNumber)
        {
            return new Question
            {
                SessionKey = session.SessionKey,
                QuestionText = questionText,
                QuestionNumber = questionNumber
            };
        }

    }
}
