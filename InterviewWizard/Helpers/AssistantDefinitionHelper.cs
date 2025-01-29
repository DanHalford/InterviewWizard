using InterviewWizard.Models.Utility;
using Microsoft.Extensions.Options;

namespace InterviewWizard.Helpers
{
    public class AssistantDefinitionHelper
    {
        private readonly Dictionary<string, string> _assistantDefinitions;

        public AssistantDefinitionHelper(IOptions<AppSettings> options)
        {
            _assistantDefinitions = options.Value.Prompts;
        }

        public string GetDefinitionContent(string key)
        {
            if (_assistantDefinitions.TryGetValue(key, out var filePath))
            {
                return File.ReadAllText(filePath);
            }

            throw new FileNotFoundException($"The definition for key {key} was not found.");
        }
    }
}
