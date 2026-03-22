namespace LottyAB.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(string message, CancellationToken cancellationToken = default);
}