using System;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Models.Helpers;

namespace Services.Helpers.MailService
{
    public class SmtpMailService : IMailService
    {
        private readonly IOptions<AppSettings> json;

        public SmtpMailService(IOptions<AppSettings> json)
        {
            this.json = json;
        }

        public void SendEmail(string toEmail, string message, string fromTitle = "", string Subject = "")
        {
            var options = json.Value.SMTP;
            toEmail = toEmail.ToLower();
            var email = new MimeMessage();
            var from = new MailboxAddress(fromTitle, options.Email);
            email.From.Add(from);

            var to = new MailboxAddress(toEmail);
            email.To.Add(to);

            email.Subject = Subject;

            var bodyBuilder = new BodyBuilder
            {
                TextBody = message
            };
            email.Body = bodyBuilder.ToMessageBody();
            var client = new SmtpClient();
            try
            {
                client.Connect(options.Host, options.Port, options.UseSSL);
                client.Authenticate(options.Email, options.Password);
                client.Send(email);
                client.Disconnect(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                client.Dispose();
            }
        }
    }
}