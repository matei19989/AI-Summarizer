namespace AISummarizerAPI.Core.Interfaces;

using AISummarizerAPI.Core.Models;

/// <summary>
/// Interface focused solely on content extraction from external sources
/// Separated from summarization concerns - a class might extract content
/// without needing to know anything about AI summarization
/// </summary>
public interface IContentExtractor
{
    /// <summary>
    /// Extracts readable content from a URL
    /// Returns domain model instead of infrastructure-specific types
    /// </summary>
    Task<ExtractedContent> ExtractAsync(string url, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a URL is accessible before attempting extraction
    /// Useful for quick validation without full extraction overhead
    /// </summary>
    Task<bool> IsAccessibleAsync(string url, CancellationToken cancellationToken = default);
}