using AngleSharp.Html;
using Azure.Core;
using Azure.Identity;
using InterviewWizard.Models.Utility;
using Microsoft.Graph;
using Microsoft.Graph.IdentityGovernance.EntitlementManagement.AccessPackages.Item.ResourceRoleScopes.Item.Role.Resource.Scopes;
using Microsoft.Graph.Me.MailFolders.Item.ChildFolders.Item.Messages.Item;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users;
using Microsoft.Graph.Users.Item.SendMail;

namespace InterviewWizard.Helpers
{
    public class EmailService
    {
        private string _fromAddress;
        private GraphServiceClient _graphClient;
        private AppSettings _appSettings;

        public EmailService(GraphServiceClient graphClient, AppSettings appSettings)
        {
            _graphClient = graphClient;
            _appSettings = appSettings;
            _fromAddress = appSettings.Email.From;
        }

        public async Task SendEmailAsync(string recipient, string subject, string body)
        {
            var messageBody = new SendMailPostRequestBody()
            {
                Message = new()
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = body
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = recipient
                            }
                        }
                    }
                }
            };

            await _graphClient.Users[_fromAddress].SendMail.PostAsync(messageBody);
        }
    }
}
