using System.Threading.Tasks;

namespace Sohba.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
    }
}
