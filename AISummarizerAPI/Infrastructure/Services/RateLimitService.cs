using AISummarizerAPI.Configuration;
using AISummarizerAPI.Infrastructure.Interfaces;

namespace AISummarizerAPI.Infrastructure.Services;

public class RateLimitService : IRateLimitService, IDisposable
{
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private readonly TimeSpan _minRequestInterval;
    private DateTime _lastRequestTime = DateTime.MinValue;

    public RateLimitService(HuggingFaceOptions options)
    {
        _rateLimitSemaphore = new SemaphoreSlim(options.RateLimit.RequestsPerMinute, options.RateLimit.RequestsPerMinute);
        _minRequestInterval = TimeSpan.FromMinutes(1.0 / options.RateLimit.RequestsPerMinute);
    }

    public async Task WaitForAvailableSlotAsync(CancellationToken cancellationToken)
    {
        await _rateLimitSemaphore.WaitAsync(cancellationToken);

        try
        {
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLastRequest < _minRequestInterval)
            {
                var waitTime = _minRequestInterval - timeSinceLastRequest;
                await Task.Delay(waitTime, cancellationToken);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _ = Task.Delay(TimeSpan.FromMinutes(1), cancellationToken)
                .ContinueWith(_ =>
                {
                    try { _rateLimitSemaphore.Release(); }
                    catch (ObjectDisposedException) { }
                }, TaskScheduler.Default);
        }
    }

    public void Dispose() => _rateLimitSemaphore?.Dispose();
}