namespace ChatApp.Modules.Notifications.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(
            string toEmail,
            string toName,
            string subject,
            string htmlBody,
            CancellationToken cancellationToken = default);
    }
}