using System.Threading.Tasks;

namespace Services.Helpers.MailService;

public interface IMailService
{
    Task SendEmailAsync(string toEmail, string message, string fromTitle = "", string Subject = "");
}
