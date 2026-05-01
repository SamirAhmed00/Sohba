using Microsoft.Extensions.Options;
using Sohba.Application.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Sohba.Infrastructure.Services
{
    public class MailtrapEmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;

        public MailtrapEmailService(IOptions<MailSettings> mailSettings)
        {
            _mailSettings = mailSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            using var client = new SmtpClient(_mailSettings.Host, _mailSettings.Port)
            {
                Credentials = new NetworkCredential(_mailSettings.UserName, _mailSettings.Password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("noreply@sohba.com", "Sohba System"),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}
