using DocumentFormat.OpenXml.Presentation;

namespace InterviewWizard.Models.Utility
{
    public class AppSettings
    {
        public struct LoggingSettings
        {
            public LogLevelSettings LogLevel { get; set; }
        }
        public struct LogLevelSettings
        {
            public string Default { get; set; }
            public string System { get; set; }
            public string Microsoft { get; set; }
        }
        public struct StorageContainerSettings
        {
            public string Url { get; set; }
            public string ContainerName { get; set; }
        }
        public struct DataStoreSettings
        {
            public string ConnectionString { get; set; }
        }
        public struct SessionManagementSettings
        {
            public string SessionTableName { get; set; }
            public string SessionObjectTableName { get; set; }
        }
        public struct GraphSettings
        {
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
            public string TenantId { get; set; }
        }
        public struct EmailSettings
        {
            public string From { get; set; }
            public Dictionary<string, string> Templates { get; set; }
        }
        public struct ReCaptchaSettings
        {
            public string SiteKey { get; set; }
            public string SecretKey { get; set; }
            public bool UseRecaptchaNet { get; set; }
            public float ScoreThreshold { get; set; }
        }
        public struct StripeSettings
        {
            public string PublishableKey { get; set; }
        }
        public struct OpenAISettings
        {
            public string ComplexModelName { get; set; }
            public string SimpleModelName { get; set; }
        }

        public LoggingSettings Logging { get; set; }
        public string? AllowedHosts { get; set; }
        public string? KeyVaultUri { get; set; }
        public StorageContainerSettings StorageContainer { get; set; }
        public DataStoreSettings DataStore { get; set; }
        public SessionManagementSettings SessionManagement { get; set; }
        public GraphSettings Graph { get; set; }
        public EmailSettings Email { get; set; }
        public ReCaptchaSettings ReCaptcha { get; set; }
        public OpenAISettings OpenAI { get; set; }
        public Dictionary<string, string>? Prompts { get; set; }
        public List<string>? KnowledgeFiles { get; set; }
    }
}
