// AISummarizerAPI/Services/Interfaces/IHuggingFaceApiClient.cs
using AISummarizerAPI.Models.HuggingFace;

namespace AISummarizerAPI.Services.Interfaces;

/// <summary>
/// Interface for Hugging Face API client operations
/// Follows Interface Segregation Principle - focused on HF-specific operations
/// Abstracts the underlying HTTP communication for better testability
/// </summary>
public interface IHuggingFaceApiClient
{
    /// <summary>
    /// Generates a text summary using the specified Hugging Face model
    /// </summary>
    /// <param name="text">The text content to summarize</param>
    /// <param name="cancellationToken">Cancellation token for request cancellation</param>
    /// <returns>Summarization result from Hugging Face API</returns>
    Task<HuggingFaceSummarizationResponse> SummarizeTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests connectivity to the Hugging Face API
    /// Useful for health checks and troubleshooting
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for request cancellation</param>
    /// <returns>True if API is accessible, false otherwise</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the API is currently available (not in loading state)
    /// Hugging Face models sometimes need time to "warm up"
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for request cancellation</param>
    /// <returns>API availability status</returns>
    Task<HuggingFaceApiStatus> GetApiStatusAsync(CancellationToken cancellationToken = default);
}