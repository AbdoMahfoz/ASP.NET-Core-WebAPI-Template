using System;
using MailKit.Net.Smtp;
using MimeKit;

namespace BusinessLogic.HelperLogic
{
    public interface IEmailSenderLogic
    {
        void SendEmail(string toEmail, string fromEmail, string message, string fromTitle = "", string Subject = "");
    }
    public class EmailSenderLogic : IEmailSenderLogic
    {
        public void SendEmail(string toEmail, string fromEmail, string message, string fromTitle = "", string Subject = "")
        {
            toEmail = toEmail.ToLower();
            var email = new MimeMessage();
            var from = new MailboxAddress(fromTitle, fromEmail);
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
                client.Connect("smtp-mail.outlook.com", 587, false);
                client.Authenticate("Talent-Management-asu@outlook.com", "TMIsTheBest5555");
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
