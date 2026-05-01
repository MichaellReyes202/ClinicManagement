using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<MailSettings> mailSettings, ILogger<EmailService> logger)
        {
            _mailSettings = mailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var message = new MailMessage
                {
                    From = new MailAddress(_mailSettings.Email, "Clínica Oficentro"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(new MailAddress(toEmail));

                using var smtpClient = new SmtpClient(_mailSettings.Host, _mailSettings.Puerto)
                {
                    Credentials = new NetworkCredential(_mailSettings.Email, _mailSettings.Password),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to {toEmail}: {ex.Message}");
                throw; // Rethrow to let the caller handle it if needed
            }
        }
    }
}
