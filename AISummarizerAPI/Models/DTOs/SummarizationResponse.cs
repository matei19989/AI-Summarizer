namespace AISummarizerAPI.Models.DTOs;

/// <summary>
/// Data Transfer Object for summarization responses
/// Encapsulates the output data from content summarization operations
/// </summary>
public class SummarizationResponse
{
    /// <summary>
    /// The generated summary text
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether audio is available for this summary
    /// </summary>
    public bool HasAudio { get; set; } = false;

    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Any error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The original content type that was processed
    /// </summary>
    public string ProcessedContentType { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the summary was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}