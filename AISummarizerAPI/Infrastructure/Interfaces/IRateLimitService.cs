namespace AISummarizerAPI.Infrastructure.Interfaces;

public interface IRateLimitService : IDisposable
{
    Task WaitForAvailableSlotAsync(CancellationToken cancellationToken);
}