namespace AISummarizerAPI.Core.Models;

/// <summary>
/// Core domain model representing the result of a summarization operation
/// This encapsulates all the business data we care about
/// </summary>
public class SummarizationResult
{
    public string Summary { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ProcessingTime { get; set; }
    public string SourceType { get; set; } = string.Empty;
    
    // Factory methods for common scenarios
    public static SummarizationResult Success(string summary, string sourceType, TimeSpan processingTime)
    {
        return new SummarizationResult
        {
            Summary = summary,
            Success = true,
            SourceType = sourceType,
            ProcessingTime = processingTime
        };
    }
    
    public static SummarizationResult Failure(string errorMessage, string sourceType)
    {
        return new SummarizationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            SourceType = sourceType
        };
    }
}