using AISummarizerAPI.Models.DTOs;

namespace AISummarizerAPI.Services.Interfaces;

/// <summary>
/// Service interface for content summarization operations
/// Follows Interface Segregation Principle - focused solely on summarization concerns
/// This abstraction allows for easy testing and future implementation changes
/// </summary>
public interface ISummarizationService
{
    /// <summary>
    /// Summarizes content from text input
    /// </summary>
    /// <param name="content">The text content to summarize</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Summarization response with generated summary</returns>
    Task<SummarizationResponse> SummarizeTextAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Summarizes content from a URL by extracting and processing the web page content
    /// </summary>
    /// <param name="url">The URL to extract content from and summarize</param>
    /// <param name="cancellationToken">Cancellation token for async operation</param>
    /// <returns>Summarization response with generated summary</returns>
    Task<SummarizationResponse> SummarizeUrlAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether the provided content is suitable for summarization
    /// Follows Single Responsibility Principle - validation logic isolated
    /// </summary>
    /// <param name="content">Content to validate</param>
    /// <param name="contentType">Type of content (text or url)</param>
    /// <returns>Validation result with any error messages</returns>
    (bool IsValid, string ErrorMessage) ValidateContent(string content, string contentType);
}