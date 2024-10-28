using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Models.Helpers;

namespace Services.Helpers.MailService
{
    public class SmtpMailService : IMailService
    {
        private readonly AppSettings _appSettings;  // Dependency injected settings
        private readonly ILogger<SmtpMailService> _logger;  // Dependency injected logger for better logging

        // Constructor with dependency injection
        public SmtpMailService(IOptions<AppSettings> settings, ILogger<SmtpMailService> logger)
        {
            _appSettings = settings.Value;  // Assign settings from IOptions
            _logger = logger;  // Assign logger
        }

        // Asynchronous method to send email
        public async Task SendEmailAsync(string toEmail, string message, string fromTitle = "", string subject = "")
        {
            var options = _appSettings.Smtp;
            toEmail = toEmail.ToLower();
            var email = new MimeMessage();
            var from = new MailboxAddress(fromTitle, options.Email);
            email.From.Add(from);

            var to = MailboxAddress.Parse(toEmail);
            email.To.Add(to);

            email.Subject = subject;

            // Supporting HTML content in the body
            var bodyBuilder = new BodyBuilder
            {
                TextBody = message,
                HtmlBody = $"<html><body>{message}</body></html>"
            };
            email.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();  // Using statement to ensure disposal
            try
            {
                await client.ConnectAsync(options.Host, options.Port, options.UseSsl);  // Asynchronous connection
                await client.AuthenticateAsync(options.Email, options.Password);  // Asynchronous authentication
                await client.SendAsync(email);  // Asynchronous send
                await client.DisconnectAsync(true);  // Asynchronous disconnect
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to send email: {ExceptionMessage}", e.Message);  // Detailed logging
                throw new ApplicationException("Error in sending email", e);  // Throwing an application-specific exception
            }
        }
    }
}
