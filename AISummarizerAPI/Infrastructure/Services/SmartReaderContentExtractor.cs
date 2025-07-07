namespace AISummarizerAPI.Infrastructure.Services;

using AISummarizerAPI.Core.Interfaces;
using AISummarizerAPI.Core.Models;
using SmartReader;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

public class SmartReaderContentExtractor : IContentExtractor
{
    private readonly ILogger<SmartReaderContentExtractor> _logger;
    private readonly HttpClient _httpClient;

    public SmartReaderContentExtractor(
        ILogger<SmartReaderContentExtractor> logger,
        HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<ExtractedContent> ExtractAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting content from URL: {Url}", SanitizeUrlForLogging(url));

        try
        {
            var reader = new Reader(url);
            
            var extractionTask = reader.GetArticleAsync();
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            
            var completedTask = await Task.WhenAny(extractionTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException(cancellationToken);
                    
                throw new TimeoutException("Content extraction timed out after 30 seconds");
            }
            
            var article = await extractionTask;

            if (article == null || string.IsNullOrWhiteSpace(article.Content))
            {
                _logger.LogWarning("No readable content found at URL: {Url}", SanitizeUrlForLogging(url));
                return ExtractedContent.Failure(
                    "Could not extract readable content from this URL. The page may not contain article content or may be behind a paywall.",
                    url);
            }

            var cleanContent = CleanExtractedHtmlContent(article.Content);
            
            if (cleanContent.Length < 50)
            {
                return ExtractedContent.Failure(
                    "Extracted content is too short for meaningful summarization (minimum 50 characters required).",
                    url);
            }

            _logger.LogInformation("Successfully extracted {ContentLength} characters from URL", cleanContent.Length);

            return ExtractedContent.Success(
                cleanContent,
                article.Title ?? "Untitled Article",
                article.Author ?? string.Empty,
                url);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("URL content extraction was cancelled");
            throw;
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP error during URL content extraction");
            return ExtractedContent.Failure(
                "Failed to access the URL. Please check that the URL is correct and accessible.",
                url);
        }
        catch (TimeoutException)
        {
            _logger.LogWarning("Content extraction timed out for URL: {Url}", SanitizeUrlForLogging(url));
            return ExtractedContent.Failure(
                "The page took too long to load. Please try a different URL.",
                url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during URL content extraction");
            return ExtractedContent.Failure(
                "An unexpected error occurred while extracting content from the URL.",
                url);
        }
    }

    public async Task<bool> IsAccessibleAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "URL accessibility check failed for: {Url}", SanitizeUrlForLogging(url));
            return false;
        }
    }

    private static string CleanExtractedHtmlContent(string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return string.Empty;

        var cleanText = Regex.Replace(htmlContent, @"<[^>]*>", " ");
        cleanText = System.Net.WebUtility.HtmlDecode(cleanText);
        cleanText = Regex.Replace(cleanText, @"\s+", " ");
        
        return cleanText.Trim();
    }

    private static string SanitizeUrlForLogging(string url)
    {
        if (string.IsNullOrEmpty(url))
            return "null";
            
        var sanitized = url.Replace("\r", "").Replace("\n", "").Trim();
        return sanitized.Length > 200 ? sanitized.Substring(0, 200) + "..." : sanitized;
    }
}