namespace AISummarizerAPI.Services.Interfaces;

/// <summary>
/// Service interface for URL content extraction operations
/// Follows Interface Segregation Principle - focused solely on URL processing
/// Abstracts the underlying extraction mechanism (Mozilla Readability)
/// </summary>
public interface IUrlContentExtractor
{
    /// <summary>
    /// Extracts readable content from a URL
    /// Uses Mozilla Readability algorithm to extract main article content
    /// </summary>
    /// <param name="url">The URL to extract content from</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Extracted article content and metadata</returns>
    Task<UrlExtractionResult> ExtractContentAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a URL is accessible and processable
    /// Performs basic connectivity and content-type validation
    /// </summary>
    /// <param name="url">The URL to validate</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Validation result with any error messages</returns>
    Task<(bool IsValid, string ErrorMessage)> ValidateUrlAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result object for URL content extraction operations
/// Encapsulates extracted content and metadata
/// </summary>
public class UrlExtractionResult
{
    /// <summary>
    /// The main article content extracted from the URL
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The title of the article/page
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Author information if available
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the extraction was successful
    /// </summary>
    public bool Success { get; set; } = false;

    /// <summary>
    /// Any error message if extraction failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The original URL that was processed
    /// </summary>
    public string SourceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the extraction was performed
    /// </summary>
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
}