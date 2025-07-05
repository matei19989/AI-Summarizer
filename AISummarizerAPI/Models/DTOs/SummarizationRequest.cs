using System.ComponentModel.DataAnnotations;

namespace AISummarizerAPI.Models.DTOs;

/// <summary>
/// Data Transfer Object for summarization requests
/// Encapsulates the input data required for content summarization
/// </summary>
public class SummarizationRequest
{
    /// <summary>
    /// The content to be summarized (either plain text or URL)
    /// </summary>
    [Required(ErrorMessage = "Content is required")]
    [StringLength(10000, ErrorMessage = "Content cannot exceed 10,000 characters")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The type of content being submitted (text or url)
    /// </summary>
    [Required(ErrorMessage = "Content type is required")]
    [RegularExpression("^(text|url)$", ErrorMessage = "Content type must be either 'text' or 'url'")]
    public string ContentType { get; set; } = string.Empty;
}