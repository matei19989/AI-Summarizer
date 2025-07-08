namespace AISummarizerAPI.Core.Interfaces;

using AISummarizerAPI.Core.Models;

/// <summary>
/// Interface focused solely on the core summarization functionality
/// This is our most important business capability - generating summaries
/// Kept separate so we can easily swap AI providers or add new ones
/// </summary>
public interface IContentSummarizer
{
    /// <summary>
    /// Generates a summary from text content
    /// Takes and returns domain models, hiding AI provider details
    /// </summary>
    Task<SummarizationResult> SummarizeAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the summarization service is available
    /// Important for health checks and graceful degradation
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}