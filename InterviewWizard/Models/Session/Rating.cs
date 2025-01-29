namespace InterviewWizard.Models.Session
{
    [Flags]
    public enum Rating
    {
        Unrated = 0,
        Good = 1,
        Bad = 2,
        Inappropriate = 4,
        //Good ratings
        Insightful = 8,
        ThoughtProvoking = 16,
        Perceptive = 32,
        Relevant = 64,
        //Poor ratings
        Vague = 128,
        Irrelevant = 256,
        Redundant = 512,
        OffTopic = 1024,
        //Inappropriate ratings
        Offensive = 2048,
        Discriminatory = 4096,
        Invasive = 8192,
        Unethical = 16384,
        Prejudiced = 32768,
    }
}
