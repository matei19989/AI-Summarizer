namespace AISummarizerAPI.Core.Models;

/// <summary>
/// Core domain model representing a content summarization request
/// This is our central business entity that all layers understand
/// </summary>
public class ContentRequest
{
    public string Content { get; set; } = string.Empty;
    public ContentType ContentType { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Factory method to create from DTOs - encapsulates creation logic
    /// </summary>
    public static ContentRequest FromText(string text)
    {
        return new ContentRequest
        {
            Content = text,
            ContentType = ContentType.Text
        };
    }

    public static ContentRequest FromUrl(string url)
    {
        return new ContentRequest
        {
            Content = url,
            ContentType = ContentType.Url
        };
    }
}