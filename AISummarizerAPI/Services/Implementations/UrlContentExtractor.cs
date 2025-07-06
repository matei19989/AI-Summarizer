using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Utils;
using SmartReader;
using System.Text.RegularExpressions;

namespace AISummarizerAPI.Services.Implementations;

/// <summary>
/// Implementation of URL content extraction using Mozilla Readability algorithm
/// Uses SmartReader library which is a .NET port of Mozilla's Readability
/// Follows the established service pattern with proper error handling and logging
/// </summary>
public class UrlContentExtractor : IUrlContentExtractor
{
    private readonly ILogger<UrlContentExtractor> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor follows Dependency Injection pattern established in the project
    /// </summary>
    public UrlContentExtractor(ILogger<UrlContentExtractor> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Extracts readable content from a URL using Mozilla Readability algorithm
    /// Maintains the same error handling patterns as SummarizationService
    /// </summary>
    public async Task<UrlExtractionResult> ExtractContentAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting content extraction for URL: {Url}", LogSanitizer.SanitizeAndTruncate(url, 200));

        try
        {
            // Validate URL accessibility first
            var (isValid, errorMessage) = await ValidateUrlAsync(url, cancellationToken);
            if (!isValid)
            {
                _logger.LogWarning("URL validation failed: {ErrorMessage}", errorMessage);
                return new UrlExtractionResult
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    SourceUrl = url
                };
            }

            // Use SmartReader to extract content with Mozilla Readability algorithm
            // This is the core Day 5 functionality - actual URL content extraction
            // Note: SmartReader doesn't support CancellationToken, so we wrap with timeout
            var reader = new Reader(url);
            
            // Create a timeout task to respect the cancellation token
            var extractionTask = reader.GetArticleAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            
            var completedTask = await Task.WhenAny(extractionTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                throw new TimeoutException("Content extraction timed out after 30 seconds");
            }
            
            var article = await extractionTask;

            if (article == null || string.IsNullOrWhiteSpace(article.Content))
            {
                _logger.LogWarning("No readable content found at URL: {Url}", LogSanitizer.SanitizeAndTruncate(url, 200));
                return new UrlExtractionResult
                {
                    Success = false,
                    ErrorMessage = "Could not extract readable content from this URL. The page may not contain article content or may be behind a paywall.",
                    SourceUrl = url
                };
            }

            // Clean and process the extracted content
            var cleanContent = CleanExtractedContent(article.Content);
            
            // Validate content length for summarization
            if (cleanContent.Length < 50)
            {
                return new UrlExtractionResult
                {
                    Success = false,
                    ErrorMessage = "Extracted content is too short for meaningful summarization (minimum 50 characters required).",
                    SourceUrl = url
                };
            }

            _logger.LogInformation("Successfully extracted {ContentLength} characters from URL", cleanContent.Length);

            return new UrlExtractionResult
            {
                Content = cleanContent,
                Title = article.Title ?? "Untitled Article",
                Author = article.Author ?? string.Empty,
                Success = true,
                SourceUrl = url,
                ExtractedAt = DateTime.UtcNow
            };

        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("URL content extraction was cancelled");
            throw;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP error during URL content extraction");
            return new UrlExtractionResult
            {
                Success = false,
                ErrorMessage = "Failed to access the URL. Please check that the URL is correct and accessible.",
                SourceUrl = url
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during URL content extraction");
            return new UrlExtractionResult
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while extracting content from the URL.",
                SourceUrl = url
            };
        }
    }

    /// <summary>
    /// Validates URL accessibility and basic content-type checking
    /// Follows the same validation pattern as existing content validation
    /// </summary>
    public async Task<(bool IsValid, string ErrorMessage)> ValidateUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic URL format validation (already done in controller, but double-checking)
            var urlPattern = @"^https?://[^\s/$.?#].[^\s]*$";
            if (!Regex.IsMatch(url, urlPattern, RegexOptions.IgnoreCase))
            {
                return (false, "Invalid URL format. Please provide a valid URL starting with http:// or https://");
            }

            // Check if URL is accessible
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return (false, $"URL is not accessible (HTTP {(int)response.StatusCode}). Please check the URL and try again.");
            }

            // Basic content-type validation - should be HTML
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("html"))
            {
                _logger.LogWarning("Non-HTML content type detected: {ContentType}", contentType);
                // Don't fail validation - some sites don't set proper content types
            }

            return (true, string.Empty);

        }
        catch (HttpRequestException)
        {
            return (false, "Could not connect to the URL. Please check your internet connection and that the URL is accessible.");
        }
        catch (TaskCanceledException)
        {
            return (false, "Request timed out. The URL may be slow to respond.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating URL: {Url}", LogSanitizer.SanitizeAndTruncate(url, 200));
            return (false, "Could not validate the URL. Please try again.");
        }
    }

    /// <summary>
    /// Cleans extracted HTML content to plain text suitable for summarization
    /// Removes HTML tags and normalizes whitespace
    /// </summary>
    private static string CleanExtractedContent(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return string.Empty;

        // Remove HTML tags
        var cleanText = Regex.Replace(htmlContent, @"<[^>]*>", " ");
        
        // Decode HTML entities
        cleanText = System.Net.WebUtility.HtmlDecode(cleanText);
        
        // Normalize whitespace
        cleanText = Regex.Replace(cleanText, @"\s+", " ");
        
        // Remove leading/trailing whitespace
        cleanText = cleanText.Trim();

        return cleanText;
    }
}
