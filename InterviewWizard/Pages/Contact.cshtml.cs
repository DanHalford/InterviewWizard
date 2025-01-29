using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using AspNetCore.ReCaptcha;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Graph;
using Microsoft.Extensions.Options;
using InterviewWizard.Controllers;
using InterviewWizard.Models.Utility;
using InterviewWizard.Models.Request;
using InterviewWizard.Helpers;

namespace InterviewWizard.Pages
{

    [ValidateReCaptcha]
    public class ContactModel : PageModel
    {
        private readonly AppSettings _appSettings;
        private readonly GraphServiceClient _graphClient;
        private readonly ILogger<ContactModel> _logger;

        public ContactModel(IOptions<AppSettings> appSettings, GraphServiceClient graphClient, ILogger<ContactModel> logger)
        {
            _appSettings = appSettings.Value;
            _graphClient = graphClient;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(ContactFormRequestModel contactForm)
        {
            _logger.BeginScope("Contact form submission");
            if (!ModelState.IsValid)
            {
                _logger.LogInformation("Invalid model state");
                return Page();
            }

            _logger.LogInformation("Model validated. Sending email");
            string templateName = _appSettings.Email.Templates["Contact"] + ".html";
            string emailTemplate = System.IO.File.ReadAllText($"Templates/{templateName}");

            emailTemplate = emailTemplate.Replace("{{ name }}", contactForm.Name);
            emailTemplate = emailTemplate.Replace("{{ email }}", contactForm.EmailAddress);
            string messageText = contactForm.Message.Replace("\n", "<p />");
            emailTemplate = emailTemplate.Replace("{{ message }}", messageText);
            EmailService emailService = new EmailService(_graphClient, _appSettings);
            await emailService.SendEmailAsync("hello@interviewwizard.ai", "Contact Form Submission", emailTemplate);

            // Send email
            return RedirectToPage("ContactSuccess");
        }
    }
}
