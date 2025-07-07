namespace AISummarizerAPI.Core.Interfaces;

using AISummarizerAPI.Core.Models;

/// <summary>
/// Interface focused solely on content validation responsibilities
/// Following Interface Segregation Principle - clients that only need validation
/// don't need to depend on summarization or extraction methods
/// </summary>
public interface IContentValidator
{
    /// <summary>
    /// Validates that content is suitable for processing
    /// Returns validation result with specific error messages for better UX
    /// </summary>
    Task<ValidationResult> ValidateAsync(ContentRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Quick synchronous validation for simple cases
    /// Useful for immediate feedback in the UI layer
    /// </summary>
    ValidationResult ValidateBasicFormat(ContentRequest request);
}