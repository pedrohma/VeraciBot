using System.Net.Mail;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using SendGrid.SmtpApi;
using VeraciBot;
using VeraciLib.Settings;

namespace VeraciApp.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger? _logger = null;
        private readonly SettingsBase _sendGridSettings;

        public EmailSender() {}

        public EmailSender(SettingsBase sendGridSettings, ILogger<EmailSender>? logger = null)
        {
            _sendGridSettings = sendGridSettings;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            if (string.IsNullOrEmpty(_sendGridSettings.ApiKey))
            {
                throw new Exception("Null SendGridKey");
            }
            await Execute(_sendGridSettings.ApiKey, subject, message, toEmail);
        }

        public async Task Execute(string apiKey, string subject, string message, string toEmail)
        {
            var client = new SendGridClient(_sendGridSettings.ApiKey);

            var msg = new SendGridMessage()
            {
                From = new EmailAddress("noreply@veraci.bot", "Não Responda (VERACIBOT)"),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message,
            };
            msg.AddTo(new EmailAddress(toEmail));

            // Disable click tracking.
            // See https://sendgrid.com/docs/User_Guide/Settings/tracking.html
            msg.SetClickTracking(false, false);
            var response = await client.SendEmailAsync(msg);

            if (_logger != null)
            {
                _logger.LogInformation(
                    response.IsSuccessStatusCode
                        ? $"Email to {toEmail} queued successfully!"
                        : $"Failure Email to {toEmail}"
                );
            }
            else
            {
                // Log to console if logger is not available
                Console.WriteLine(
                    response.IsSuccessStatusCode
                        ? $"Email to {toEmail} queued successfully!"
                        : $"Failure Email to {toEmail}"
                );
            }
        }
    }
}
