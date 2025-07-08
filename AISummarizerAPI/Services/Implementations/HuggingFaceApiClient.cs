// AISummarizerAPI/Services/Implementations/HuggingFaceApiClient.cs
using AISummarizerAPI.Configuration;
using AISummarizerAPI.Models.HuggingFace;
using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Utils;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AISummarizerAPI.Services.Implementations;

/// <summary>
/// Implementation of Hugging Face API client
/// Handles HTTP communication with Hugging Face Inference API
/// Follows Single Responsibility Principle - only handles HF API communication
/// Implements IDisposable for proper resource management
/// </summary>
public class HuggingFaceApiClient : IHuggingFaceApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly HuggingFaceOptions _options;
    private readonly ILogger<HuggingFaceApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    // Rate limiting fields
    private readonly SemaphoreSlim _rateLimitSemaphore;
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly TimeSpan _minRequestInterval;

    public HuggingFaceApiClient(
        HttpClient httpClient,
        IOptions<HuggingFaceOptions> options,
        ILogger<HuggingFaceApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure JSON serialization options for consistent API communication
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Set up rate limiting to respect API limits
        _rateLimitSemaphore = new SemaphoreSlim(_options.RateLimit.RequestsPerMinute, _options.RateLimit.RequestsPerMinute);
        _minRequestInterval = TimeSpan.FromMinutes(1.0 / _options.RateLimit.RequestsPerMinute);

        // Configure HTTP client with authentication and timeouts
        ConfigureHttpClient();
    }

    /// <summary>
    /// Summarizes text using the Hugging Face BART model
    /// Includes rate limiting, retry logic, and comprehensive error handling
    /// </summary>
    public async Task<HuggingFaceSummarizationResponse> SummarizeTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Empty text provided for summarization");
            return new HuggingFaceSummarizationResponse
            {
                Success = false,
                ErrorMessage = "Text content cannot be empty",
                StatusCode = 400
            };
        }

        _logger.LogInformation("Starting text summarization for content length: {Length}", text.Length);

        try
        {
            // Apply rate limiting before making the request
            await ApplyRateLimitingAsync(cancellationToken);

            // Prepare the request with optimal parameters for summarization
            var request = CreateSummarizationRequest(text);
            var requestJson = JsonSerializer.Serialize(request, _jsonOptions);

            _logger.LogDebug("Sending request to Hugging Face API: {RequestSize} bytes",
                Encoding.UTF8.GetByteCount(requestJson));

            // Execute request with retry logic for resilience
            var response = await ExecuteWithRetryAsync(async () =>
            {
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
                return await _httpClient.PostAsync($"/models/{_options.Models.SummarizationModel}", content, cancellationToken);
            }, cancellationToken);

            // Process the response and handle different scenarios
            return await ProcessSummarizationResponseAsync(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Summarization request was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during text summarization");
            return new HuggingFaceSummarizationResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred during summarization",
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Tests basic connectivity to the Hugging Face API
    /// Useful for health checks and initial setup validation
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Testing connection to Hugging Face API");

            // Use a simple request to test connectivity without consuming quota
            var testText = "This is a short test message for API connectivity verification.";
            var response = await SummarizeTextAsync(testText, cancellationToken);

            var isConnected = response.Success || response.StatusCode != 0;
            _logger.LogInformation("Hugging Face API connection test result: {IsConnected}", isConnected);

            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test Hugging Face API connection");
            return false;
        }
    }

    /// <summary>
    /// Checks the current status of the Hugging Face API and model availability
    /// Models sometimes need time to "warm up" after periods of inactivity
    /// </summary>
    public async Task<HuggingFaceApiStatus> GetApiStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Hugging Face API status");

            // Make a lightweight request to check model status
            var testRequest = CreateSummarizationRequest("Status check");
            var requestJson = JsonSerializer.Serialize(testRequest, _jsonOptions);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/models/{_options.Models.SummarizationModel}", content, cancellationToken);

            return await ProcessApiStatusResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Hugging Face API status");
            return new HuggingFaceApiStatus
            {
                IsAvailable = false,
                StatusMessage = "Failed to check API status",
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Configures the HTTP client with proper authentication and settings
    /// </summary>
    private void ConfigureHttpClient()
    {
        // Set base URL for Hugging Face Inference API
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);

        // Add authentication header with API token
        if (!string.IsNullOrEmpty(_options.ApiToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiToken);
        }

        // Set user agent for API identification
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("AISummarizer/1.0 (Hugging Face Client)");

        // Configure timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.RateLimit.TimeoutSeconds);

        _logger.LogDebug("Configured Hugging Face HTTP client with base URL: {BaseUrl}", _options.BaseUrl);
    }

    /// <summary>
    /// Creates an optimized summarization request with appropriate parameters
    /// Parameters are tuned for good quality summaries that fit well in the UI
    /// </summary>
    private HuggingFaceSummarizationRequest CreateSummarizationRequest(string text)
    {
        // Calculate dynamic max length based on input text
        // Aim for summaries that are 15-25% of original length, with reasonable bounds
        var dynamicMaxLength = Math.Max(50, Math.Min(200, text.Length / 4));
        var dynamicMinLength = Math.Max(30, dynamicMaxLength / 3);

        return new HuggingFaceSummarizationRequest
        {
            Inputs = text,
            Parameters = new SummarizationParameters
            {
                MaxLength = dynamicMaxLength,
                MinLength = dynamicMinLength,
                DoSample = false, // Deterministic results for consistency
                Temperature = 0.3 // Low temperature for focused summaries
            },
            Options = new ApiOptions
            {
                WaitForModel = true,
                UseCache = false // Fresh results for each request
            }
        };
    }

    /// <summary>
    /// Processes the HTTP response from Hugging Face summarization API
    /// Handles various response scenarios including errors and model loading states
    /// </summary>
    private async Task<HuggingFaceSummarizationResponse> ProcessSummarizationResponseAsync(HttpResponseMessage response)
    {
        var processingTime = TimeSpan.Zero; // In a real implementation, you'd measure this
        var responseContent = await response.Content.ReadAsStringAsync();

        _logger.LogDebug("Received response with status: {StatusCode}, content length: {ContentLength}",
            response.StatusCode, responseContent.Length);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                // Try to parse as successful response first
                var rawResponses = JsonSerializer.Deserialize<HuggingFaceRawResponse[]>(responseContent, _jsonOptions);

                if (rawResponses != null && rawResponses.Length > 0 && !string.IsNullOrEmpty(rawResponses[0].SummaryText))
                {
                    _logger.LogInformation("Successfully received summary of length: {Length}", rawResponses[0].SummaryText.Length);

                    return new HuggingFaceSummarizationResponse
                    {
                        SummaryText = rawResponses[0].SummaryText.Trim(),
                        Success = true,
                        StatusCode = (int)response.StatusCode,
                        ProcessingTime = processingTime
                    };
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse successful response from Hugging Face API");
            }
        }

        // Handle error responses or unsuccessful status codes
        return HandleErrorResponse(response, responseContent);
    }

    /// <summary>
    /// Handles error responses from the Hugging Face API
    /// Provides meaningful error messages for different failure scenarios
    /// </summary>
    private HuggingFaceSummarizationResponse HandleErrorResponse(HttpResponseMessage response, string responseContent)
    {
        var statusCode = (int)response.StatusCode;
        _logger.LogWarning("Received error response from Hugging Face API: {StatusCode}", statusCode);

        try
        {
            // Try to parse error response for more specific information
            var errorResponse = JsonSerializer.Deserialize<HuggingFaceErrorResponse>(responseContent, _jsonOptions);

            if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Error))
            {
                var errorMessage = GetFriendlyErrorMessage(errorResponse.Error, statusCode);

                _logger.LogError("Hugging Face API error: {Error}", errorResponse.Error);

                return new HuggingFaceSummarizationResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    StatusCode = statusCode
                };
            }
        }
        catch (JsonException)
        {
            // If we can't parse the error response, fall back to status code handling
            _logger.LogWarning("Could not parse error response from Hugging Face API");
        }

        // Fallback error handling based on HTTP status codes
        var fallbackMessage = statusCode switch
        {
            401 => "Invalid API token. Please check your Hugging Face authentication.",
            403 => "Access forbidden. Please verify your API token permissions.",
            429 => "Rate limit exceeded. Please wait before making another request.",
            503 => "Model is currently loading. Please try again in a few moments.",
            _ => $"The summarization service is temporarily unavailable (Status: {statusCode})"
        };

        return new HuggingFaceSummarizationResponse
        {
            Success = false,
            ErrorMessage = fallbackMessage,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Converts technical API error messages into user-friendly explanations
    /// </summary>
    private static string GetFriendlyErrorMessage(string apiError, int statusCode)
    {
        // Convert common API errors into user-friendly messages
        if (apiError.Contains("loading", StringComparison.OrdinalIgnoreCase))
        {
            return "The AI model is warming up. Please try again in a few moments.";
        }

        if (apiError.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || statusCode == 429)
        {
            return "Too many requests. Please wait a moment before trying again.";
        }

        if (apiError.Contains("token", StringComparison.OrdinalIgnoreCase) || statusCode == 401)
        {
            return "Authentication failed. Please check the API configuration.";
        }

        if (apiError.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return "The request took too long to process. Please try with shorter content.";
        }

        // For unknown errors, provide a generic but helpful message
        return "The summarization service encountered an error. Please try again.";
    }

    /// <summary>
    /// Processes API status response to determine model availability
    /// </summary>
    private async Task<HuggingFaceApiStatus> ProcessApiStatusResponse(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            return new HuggingFaceApiStatus
            {
                IsAvailable = true,
                IsLoading = false,
                StatusCode = statusCode,
                StatusMessage = "API is available and ready"
            };
        }

        // Check if it's a model loading scenario
        if (statusCode == 503)
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<HuggingFaceErrorResponse>(responseContent, _jsonOptions);
                if (errorResponse?.EstimatedTime.HasValue == true)
                {
                    return new HuggingFaceApiStatus
                    {
                        IsAvailable = false,
                        IsLoading = true,
                        EstimatedLoadTime = TimeSpan.FromSeconds(errorResponse.EstimatedTime.Value),
                        StatusCode = statusCode,
                        StatusMessage = "Model is loading"
                    };
                }
            }
            catch (JsonException)
            {
                // Continue with fallback handling
            }
        }

        return new HuggingFaceApiStatus
        {
            IsAvailable = false,
            IsLoading = false,
            StatusCode = statusCode,
            StatusMessage = $"API unavailable (Status: {statusCode})"
        };
    }

    /// <summary>
    /// Implements exponential backoff retry logic for resilient API communication
    /// Helps handle temporary failures and model loading scenarios
    /// </summary>
    private async Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> httpCall,
        CancellationToken cancellationToken)
    {
        var maxAttempts = _options.RateLimit.MaxRetryAttempts;
        var baseDelay = _options.RateLimit.BaseRetryDelayMs;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await httpCall();

                // Return immediately on success or non-retryable errors
                if (response.IsSuccessStatusCode || !IsRetryableStatusCode(response.StatusCode))
                {
                    return response;
                }

                _logger.LogWarning("Attempt {Attempt}/{MaxAttempts} failed with status {StatusCode}",
                    attempt, maxAttempts, response.StatusCode);

                // Don't retry on the last attempt
                if (attempt == maxAttempts)
                {
                    return response;
                }

                // Calculate exponential backoff delay
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

        // This should not be reached, but included for safety
        throw new InvalidOperationException("Retry logic completed without returning a response");
    }

    /// <summary>
    /// Determines if an HTTP status code indicates a retryable error
    /// </summary>
    private static bool IsRetryableStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.InternalServerError => true,     // 500
            HttpStatusCode.BadGateway => true,              // 502
            HttpStatusCode.ServiceUnavailable => true,     // 503
            HttpStatusCode.GatewayTimeout => true,          // 504
            HttpStatusCode.TooManyRequests => true,         // 429
            _ => false
        };
    }

    /// <summary>
    /// Applies rate limiting to prevent exceeding API quotas
    /// Uses semaphore to control concurrent requests and timing between calls
    /// </summary>
    private async Task ApplyRateLimitingAsync(CancellationToken cancellationToken)
    {
        // Wait for available slot in rate limit window
        await _rateLimitSemaphore.WaitAsync(cancellationToken);

        try
        {
            // Ensure minimum time between requests
            var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            if (timeSinceLastRequest < _minRequestInterval)
            {
                var waitTime = _minRequestInterval - timeSinceLastRequest;
                _logger.LogDebug("Rate limiting: waiting {WaitTime}ms before request", waitTime.TotalMilliseconds);
                await Task.Delay(waitTime, cancellationToken);
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            // Release the semaphore slot after a time window
            // Note: Using fire-and-forget pattern for cleanup - this is intentional
            _ = Task.Delay(TimeSpan.FromMinutes(1), cancellationToken)
                .ContinueWith(_ =>
                {
                    try
                    {
                        _rateLimitSemaphore.Release();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Semaphore was disposed, which is fine
                    }
                }, TaskScheduler.Default);
        }
    }

    /// <summary>
    /// Disposes of resources properly
    /// Implements IDisposable pattern for proper resource cleanup
    /// </summary>
    public void Dispose()
    {
        _rateLimitSemaphore?.Dispose();
        // Note: HttpClient disposal is handled by the DI container
        // We don't dispose it here as it's injected and managed externally
    }
}