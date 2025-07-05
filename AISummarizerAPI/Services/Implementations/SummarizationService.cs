using AISummarizerAPI.Models.DTOs;
using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Utils;
using System.Text.RegularExpressions;

namespace AISummarizerAPI.Services.Implementations;

/// <summary>
/// Implementation of summarization service
/// This follows the Single Responsibility Principle - handles only summarization logic
/// Future integration with Hugging Face API will happen here without affecting other layers
/// </summary>
public class SummarizationService : ISummarizationService
{
    private readonly ILogger<SummarizationService> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor demonstrates Dependency Injection principle
    /// Dependencies are injected rather than created internally
    /// </summary>
    public SummarizationService(ILogger<SummarizationService> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Summarizes plain text content
    /// Currently returns a mock response - will be replaced with Hugging Face integration
    /// </summary>
    public async Task<SummarizationResponse> SummarizeTextAsync(string content, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting text summarization for content length: {ContentLength}", content.Length);

        try
        {
            // Validate input
            var (isValid, errorMessage) = ValidateContent(content, "text");
            if (!isValid)
            {
                _logger.LogWarning("Text validation failed: {ErrorMessage}", errorMessage);
                return new SummarizationResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    ProcessedContentType = "text"
                };
            }

            // Simulate processing time for realistic user experience
            await Task.Delay(1500, cancellationToken);

            // TODO: Replace with actual Hugging Face API call
            var mockSummary = GenerateMockSummary(content, "text");

            _logger.LogInformation("Text summarization completed successfully");

            return new SummarizationResponse
            {
                Summary = mockSummary,
                HasAudio = true, // Will be determined by TTS availability later
                Success = true,
                ProcessedContentType = "text",
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Text summarization was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during text summarization");
            return new SummarizationResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while processing your text.",
                ProcessedContentType = "text"
            };
        }
    }

    /// <summary>
    /// Summarizes content from a URL
    /// Demonstrates separation of concerns - URL extraction vs summarization
    /// </summary>
    public async Task<SummarizationResponse> SummarizeUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting URL summarization for: {Url}", LogSanitizer.SanitizeAndTruncate(url, 200));

        try
        {
            // Validate input
            var (isValid, errorMessage) = ValidateContent(url, "url");
            if (!isValid)
            {
                _logger.LogWarning("URL validation failed: {ErrorMessage}", errorMessage);
                return new SummarizationResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    ProcessedContentType = "url"
                };
            }

            // Simulate URL content extraction and processing
            await Task.Delay(2500, cancellationToken);

            // TODO: Implement actual URL content extraction (ScrapingBee API or Mozilla Readability)
            // TODO: Then pass extracted content to Hugging Face summarization API
            var mockSummary = GenerateMockSummary(url, "url");

            _logger.LogInformation("URL summarization completed successfully");

            return new SummarizationResponse
            {
                Summary = mockSummary,
                HasAudio = true,
                Success = true,
                ProcessedContentType = "url",
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("URL summarization was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during URL summarization");
            return new SummarizationResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while processing the URL.",
                ProcessedContentType = "url"
            };
        }
    }

    /// <summary>
    /// Validates content based on type
    /// Follows Single Responsibility - isolated validation logic
    /// </summary>
    public (bool IsValid, string ErrorMessage) ValidateContent(string content, string contentType)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (false, "Content cannot be empty");
        }

        switch (contentType.ToLowerInvariant())
        {
            case "text":
                if (content.Length < 50)
                {
                    return (false, "Text content must be at least 50 characters for meaningful summarization");
                }
                if (content.Length > 10000)
                {
                    return (false, "Text content exceeds maximum length of 10,000 characters");
                }
                break;

            case "url":
                var urlPattern = @"^https?://[^\s/$.?#].[^\s]*$";
                if (!Regex.IsMatch(content, urlPattern, RegexOptions.IgnoreCase))
                {
                    return (false, "Please provide a valid URL starting with http:// or https://");
                }
                break;

            default:
                return (false, "Content type must be either 'text' or 'url'");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Generates mock summary for development purposes
    /// Will be removed when real AI integration is implemented
    /// </summary>
    private static string GenerateMockSummary(string content, string contentType)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var contentLength = contentType == "url" ? "URL content" : $"{content.Length} characters";

        return $"[Generated at {timestamp}] This is an AI-generated summary of your {contentType} content ({contentLength}). " +
               "The summary demonstrates the successful communication between your React frontend and C# backend API. " +
               "In the next development phase, this will be replaced with actual AI-powered summarization using the " +
               "Hugging Face facebook/bart-large-cnn model, providing intelligent, concise summaries of your content.";
    }
}