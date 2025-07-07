namespace AISummarizerAPI.Infrastructure.Services;

using AISummarizerAPI.Core.Interfaces;
using AISummarizerAPI.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Linq;

public class ContentValidationService : IContentValidator
{
    private readonly ILogger<ContentValidationService> _logger;
    private readonly HttpClient _httpClient;
    
    private const int MinTextLength = 50;
    private const int MaxTextLength = 10000;
    private const int UrlTimeoutSeconds = 10;

    public ContentValidationService(ILogger<ContentValidationService> logger, HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<ValidationResult> ValidateAsync(ContentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting validation for content type: {ContentType}", request.ContentType);

        var basicValidation = ValidateBasicFormat(request);
        if (!basicValidation.IsValid)
        {
            return basicValidation;
        }

        if (request.ContentType == ContentType.Url)
        {
            return await ValidateUrlAccessibilityAsync(request.Content, cancellationToken);
        }

        return ValidationResult.Success();
    }

    public ValidationResult ValidateBasicFormat(ContentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return ValidationResult.Failure(
                "Content cannot be empty. Please provide text to summarize or a valid URL.", 
                ValidationErrorType.EmptyContent);
        }

        return request.ContentType switch
        {
            ContentType.Text => ValidateTextContent(request.Content),
            ContentType.Url => ValidateUrlFormat(request.Content),
            _ => ValidationResult.Failure(
                $"Unsupported content type: {request.ContentType}", 
                ValidationErrorType.UnsupportedContentType)
        };
    }

    private ValidationResult ValidateTextContent(string content)
    {
        var trimmedContent = content.Trim();

        if (trimmedContent.Length < MinTextLength)
        {
            return ValidationResult.Failure(
                $"Text content must be at least {MinTextLength} characters for meaningful AI summarization. " +
                $"Current length: {trimmedContent.Length} characters.",
                ValidationErrorType.ContentTooShort);
        }

        if (trimmedContent.Length > MaxTextLength)
        {
            return ValidationResult.Failure(
                $"Text content exceeds maximum length of {MaxTextLength:N0} characters. " +
                $"Current length: {trimmedContent.Length:N0} characters. Please shorten your text.",
                ValidationErrorType.ContentTooLong);
        }

        return ValidationResult.Success();
    }

    private ValidationResult ValidateUrlFormat(string url)
    {
        var urlPattern = @"^https?://[^\s/$.?#].[^\s]*$";
        
        if (!Regex.IsMatch(url.Trim(), urlPattern, RegexOptions.IgnoreCase))
        {
            return ValidationResult.Failure(
                "Please provide a valid URL starting with http:// or https://. " +
                "Example: https://example.com/article",
                ValidationErrorType.InvalidUrlFormat);
        }

        return ValidationResult.Success();
    }

    private async Task<ValidationResult> ValidateUrlAccessibilityAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(UrlTimeoutSeconds);
            
            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AISummarizer/1.0)");
            }

            _logger.LogDebug("Checking URL accessibility: {Url}", url);

            using var response = await _httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, url), 
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("URL accessibility check failed: {StatusCode} for {Url}", 
                    response.StatusCode, url);
                
                return ValidationResult.Failure(
                    $"The URL is not accessible (HTTP {(int)response.StatusCode}). " +
                    "Please check the URL and ensure it's publicly available.",
                    ValidationErrorType.NetworkAccessibility);
            }

            _logger.LogDebug("URL accessibility confirmed for: {Url}", url);
            return ValidationResult.Success();
        }
        catch (TaskCanceledException)
        {
            return ValidationResult.Failure(
                "The URL took too long to respond. Please try a different URL or check your connection.",
                ValidationErrorType.NetworkAccessibility);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error during URL accessibility check for: {Url}", url);
            return ValidationResult.Failure(
                "Could not connect to the URL. Please verify the URL is correct and accessible.",
                ValidationErrorType.NetworkAccessibility);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during URL validation for: {Url}", url);
            return ValidationResult.Failure(
                "Could not validate the URL. Please try again.",
                ValidationErrorType.NetworkAccessibility);
        }
    }
}