using System.Net;
using AISummarizerAPI.Configuration;
using AISummarizerAPI.Infrastructure.Interfaces;
using Microsoft.Extensions.Options;

namespace AISummarizerAPI.Infrastructure.Services;

public class RetryPolicyService : IRetryPolicyService
{
    private readonly HuggingFaceOptions _options;
    private readonly ILogger<RetryPolicyService> _logger;

    public RetryPolicyService(IOptions<HuggingFaceOptions> options, ILogger<RetryPolicyService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> operation,
        CancellationToken cancellationToken)
    {
        var maxAttempts = _options.RateLimit.MaxRetryAttempts;
        var baseDelay = _options.RateLimit.BaseRetryDelayMs;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await operation();

                if (response.IsSuccessStatusCode || !IsRetryableStatusCode(response.StatusCode))
                    return response;

                _logger.LogWarning("Attempt {Attempt}/{MaxAttempts} failed with status {StatusCode}",
                    attempt, maxAttempts, response.StatusCode);

                if (attempt == maxAttempts)
                    return response;

                var delay = TimeSpan.FromMilliseconds(baseDelay * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "HTTP request failed on attempt {Attempt}/{MaxAttempts}", attempt, maxAttempts);
                var delay = TimeSpan.FromMilliseconds(baseDelay * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException("Retry logic completed without returning a response");
    }

    private static bool IsRetryableStatusCode(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.InternalServerError => true,
        HttpStatusCode.BadGateway => true,
        HttpStatusCode.ServiceUnavailable => true,
        HttpStatusCode.GatewayTimeout => true,
        HttpStatusCode.TooManyRequests => true,
        _ => false
    };
}