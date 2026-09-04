using Microsoft.Extensions.Logging;
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
        private readonly ILogger<MailtrapEmailService> _logger;
        public MailtrapEmailService(IOptions<MailSettings> mailSettings, ILogger<MailtrapEmailService> logger)
        {
            _mailSettings = mailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            var timeoutDuration = TimeSpan.FromSeconds(_mailSettings.TimeoutSeconds > 0 ? _mailSettings.TimeoutSeconds : 10);
            using var cts = new CancellationTokenSource(timeoutDuration);

            try
            {
                using var client = new SmtpClient(_mailSettings.Host, _mailSettings.Port)
                {
                    Credentials = new NetworkCredential(_mailSettings.UserName, _mailSettings.Password),
                    EnableSsl = true,
                    Timeout = (int)timeoutDuration.TotalMilliseconds
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("noreply@sohba.com", "Sohba System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage, cts.Token);


                _logger.LogInformation("Email sent to {ToEmail}, subject: {Subject}", toEmail, subject);
            }
            catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
            {
                _logger.LogError(ex, "SMTP timeout after {Seconds}s while sending email to {ToEmail}", timeoutDuration.TotalSeconds, toEmail);
                throw new TimeoutException($"Email delivery to {toEmail} timed out after {timeoutDuration.TotalSeconds} seconds.", ex);                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}, subject: {Subject}", toEmail, subject);
                throw; // Re-throw so the caller can handle the failure
            }
        }
    }
}
