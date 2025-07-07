namespace AISummarizerAPI.Core.Models;

/// <summary>
/// Domain model representing content extracted from a URL
/// Contains both the content and metadata about the extraction
/// </summary>
public class ExtractedContent
{
    public string Content { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    public static ExtractedContent CreateSuccess(string content, string title, string author, string sourceUrl)
    {
        return new ExtractedContent
        {
            Content = content,
            Title = title,
            Author = author,
            SourceUrl = sourceUrl,
            Success = true
        };
    }
    
    public static ExtractedContent CreateFailure(string errorMessage, string sourceUrl)
    {
        return new ExtractedContent
        {
            Success = false,
            ErrorMessage = errorMessage,
            SourceUrl = sourceUrl
        };
    }
}