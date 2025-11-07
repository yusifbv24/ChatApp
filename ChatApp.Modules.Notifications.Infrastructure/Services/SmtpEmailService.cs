using ChatApp.Modules.Notifications.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ChatApp.Modules.Notifications.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(
            IConfiguration configuration,
            ILogger<SmtpEmailService> logger)
        {
            var emailSettings = configuration.GetSection("EmailSettings");

            _smtpHost = emailSettings["SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
            _smtpUsername = emailSettings["SmtpUsername"] ?? "";
            _smtpPassword = emailSettings["SmtpPassword"] ?? "";
            _fromEmail = emailSettings["FromEmail"] ?? "noreply@chatapp.com";
            _fromName = emailSettings["FromName"] ?? "ChatApp";

            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_fromName, _fromEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls, cancellationToken);

                if (!string.IsNullOrEmpty(_smtpUsername) && !string.IsNullOrEmpty(_smtpPassword))
                {
                    await client.AuthenticateAsync(_smtpUsername, _smtpPassword, cancellationToken);
                }

                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return false;
            }
        }
    }
}