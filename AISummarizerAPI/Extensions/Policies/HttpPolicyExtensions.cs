// AISummarizerAPI/Extensions/Policies/HttpPolicyExtensions.cs
using Polly;
using System.Net;

namespace AISummarizerAPI.Extensions.Policies;

/// <summary>
/// Centralized HTTP resilience policies using Polly
/// Provides consistent retry, circuit breaker, and timeout policies across all HTTP clients
/// </summary>
public static class HttpPolicyExtensions
{
    /// <summary>
    /// Creates a retry policy with exponential backoff for HTTP requests
    /// </summary>
    /// <param name="policyName">Name for logging purposes</param>
    /// <param name="retryCount">Number of retry attempts (default: 3)</param>
    /// <returns>Configured retry policy</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(string policyName, int retryCount = 3)
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && IsRetryableStatusCode(r.StatusCode))
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    if (logger != null)
                    {
                        logger.LogWarning("🔄 {PolicyName} retry attempt {RetryCount} after {Delay}s. Reason: {Reason}",
                            policyName, retryCount, timespan.TotalSeconds, GetFailureReason(outcome));
                    }
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy to prevent cascading failures
    /// </summary>
    /// <param name="policyName">Name for logging purposes</param>
    /// <param name="handledEventsAllowedBeforeBreaking">Failures before opening circuit (default: 5)</param>
    /// <param name="durationOfBreak">How long to keep circuit open (default: 30s)</param>
    /// <returns>Configured circuit breaker policy</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(
        string policyName,
        int handledEventsAllowedBeforeBreaking = 5,
        TimeSpan? durationOfBreak = null)
    {
        var breakDuration = durationOfBreak ?? TimeSpan.FromSeconds(30);

        return Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: handledEventsAllowedBeforeBreaking,
                durationOfBreak: breakDuration,
                onBreak: (result, timespan) =>
                {
                    Console.WriteLine($"🚫 {policyName} circuit breaker OPENED. Will retry after {timespan.TotalSeconds}s.");
                },
                onReset: () =>
                {
                    Console.WriteLine($"✅ {policyName} circuit breaker CLOSED. Normal operation resumed.");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine($"🔍 {policyName} circuit breaker HALF-OPEN. Testing connection...");
                });
    }

    /// <summary>
    /// Creates a timeout policy for HTTP requests
    /// </summary>
    /// <param name="timeout">Request timeout duration</param>
    /// <returns>Configured timeout policy</returns>
    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(timeout);
    }

    /// <summary>
    /// Determines if an HTTP status code should trigger a retry
    /// </summary>
    private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.InternalServerError => true,     // 500
            HttpStatusCode.BadGateway => true,              // 502  
            HttpStatusCode.ServiceUnavailable => true,     // 503
            HttpStatusCode.GatewayTimeout => true,          // 504
            HttpStatusCode.RequestTimeout => true,         // 408
            HttpStatusCode.TooManyRequests => true,         // 429
            _ => false
        };
    }

    /// <summary>
    /// Gets a human-readable failure reason for logging
    /// </summary>
    private static string GetFailureReason(DelegateResult<HttpResponseMessage> outcome)
    {
        if (outcome.Exception != null)
        {
            return $"{outcome.Exception.GetType().Name}: {outcome.Exception.Message}";
        }

        if (outcome.Result != null)
        {
            return $"HTTP {(int)outcome.Result.StatusCode} {outcome.Result.StatusCode}";
        }

        return "Unknown failure";
    }
}

/// <summary>
/// Extension methods for Polly context to access logging
/// </summary>
public static class PollyContextExtensions
{
    private const string LoggerKey = "ILogger";

    public static Context WithLogger(this Context context, ILogger logger)
    {
        context[LoggerKey] = logger;
        return context;
    }

    public static ILogger? GetLogger(this Context context)
    {
        return context.TryGetValue(LoggerKey, out var logger) ? logger as ILogger : null;
    }
}