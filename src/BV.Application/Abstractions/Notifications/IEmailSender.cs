namespace BV.Application.Abstractions.Notifications;

public interface IEmailSender
{
    Task SendAsync(string email, string subject, string message, CancellationToken cancellationToken = default);
}
