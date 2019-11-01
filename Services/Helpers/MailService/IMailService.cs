
namespace Services.MailService
{
    public interface IMailService
    {
        void SendEmail(string toEmail, string fromEmail, string message, string fromTitle = "", string Subject = "");
    }
}
