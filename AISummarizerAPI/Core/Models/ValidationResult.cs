namespace AISummarizerAPI.Core.Models;

/// <summary>
/// Value object representing the result of content validation
/// Encapsulates both success/failure and provides detailed feedback
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public ValidationErrorType ErrorType { get; set; }

    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    public static ValidationResult Failure(string message, ValidationErrorType errorType)
    {
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = message,
            ErrorType = errorType
        };
    }
}
