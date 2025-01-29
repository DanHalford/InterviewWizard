using Microsoft.AspNetCore.Mvc.Rendering;

namespace InterviewWizard.Helpers
{
    public static class Extensions
    {
        /// <summary>
        /// Extension method to check if the application is running in debug mode. Utilises preprocessor directives, rather than anything in web.config.
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <returns>True is in debug mode, false if in production.</returns>
        public static bool IsDebug(this IHtmlHelper htmlHelper)
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        public static bool HasContent(this string content)
        {
            return !string.IsNullOrWhiteSpace(content);
        }

        public static bool IsEmpty(this string content)
        {
            return string.IsNullOrWhiteSpace(content);
        }
    }
}
