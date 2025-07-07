namespace AISummarizerAPI.Core.Models;

/// <summary>
/// Enum representing different types of validation errors
/// Allows for more sophisticated error handling and user feedback
/// </summary>
public enum ValidationErrorType
{
    EmptyContent,
    ContentTooShort,
    ContentTooLong,
    InvalidUrlFormat,
    UnsupportedContentType,
    NetworkAccessibility
}