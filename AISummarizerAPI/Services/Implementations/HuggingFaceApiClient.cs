using System;
using AISummarizerAPI.Configuration;
using AISummarizerAPI.Infrastructure.Interfaces;
using AISummarizerAPI.Models.HuggingFace;
using AISummarizerAPI.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AISummarizerAPI.Services.Implementations;

public class HuggingFaceApiClient : IHuggingFaceApiClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly HuggingFaceOptions _options;
    private readonly IRateLimitService _rateLimitService;
    private readonly IRetryPolicyService _retryService;
    private readonly IHuggingFaceRequestBuilder _requestBuilder;
    private readonly IHuggingFaceResponseProcessor _responseProcessor;
    private readonly ILogger<HuggingFaceApiClient> _logger;

    public HuggingFaceApiClient(
        HttpClient httpClient,
        IOptions<HuggingFaceOptions> options,
        IRateLimitService rateLimitService,
        IRetryPolicyService retryService,
        IHuggingFaceRequestBuilder requestBuilder,
        IHuggingFaceResponseProcessor responseProcessor,
        ILogger<HuggingFaceApiClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _rateLimitService = rateLimitService ?? throw new ArgumentNullException(nameof(rateLimitService));
        _retryService = retryService ?? throw new ArgumentNullException(nameof(retryService));
        _requestBuilder = requestBuilder ?? throw new ArgumentNullException(nameof(requestBuilder));
        _responseProcessor = responseProcessor ?? throw new ArgumentNullException(nameof(responseProcessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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
            await _rateLimitService.WaitForAvailableSlotAsync(cancellationToken);

            var request = _requestBuilder.CreateSummarizationRequest(text);
            var content = _requestBuilder.CreateHttpContent(request);

            _logger.LogDebug("Sending request to Hugging Face API");

            var response = await _retryService.ExecuteWithRetryAsync(async () =>
            {
                return await _httpClient.PostAsync($"/models/{_options.Models.SummarizationModel}", content, cancellationToken);
            }, cancellationToken);

            return await _responseProcessor.ProcessSummarizationResponseAsync(response);
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

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Testing connection to Hugging Face API");
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

    public async Task<HuggingFaceApiStatus> GetApiStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Hugging Face API status");

            var testRequest = _requestBuilder.CreateSummarizationRequest("Status check");
            var content = _requestBuilder.CreateHttpContent(testRequest);

            var response = await _httpClient.PostAsync($"/models/{_options.Models.SummarizationModel}", content, cancellationToken);

            return await _responseProcessor.ProcessApiStatusResponseAsync(response);
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

    public void Dispose()
    {
        _rateLimitService?.Dispose();
    }
}