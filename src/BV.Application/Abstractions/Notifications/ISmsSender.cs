namespace BV.Application.Abstractions.Notifications;

public interface ISmsSender
{
    Task SendAsync(string phone, string message, CancellationToken cancellationToken = default);
}
