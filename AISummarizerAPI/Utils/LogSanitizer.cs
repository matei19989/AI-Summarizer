namespace AISummarizerAPI.Utils;

/// <summary>
/// Utility class for sanitizing user input before logging to prevent log injection attacks
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes input by removing newline characters and other potentially dangerous characters
    /// </summary>
    /// <param name="input">The input string to sanitize</param>
    /// <returns>Sanitized string safe for logging</returns>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return "null";
            
        return input
            .Replace("\r", "")      // Remove carriage return
            .Replace("\n", "")      // Remove line feed
            .Replace("\t", " ")     // Replace tabs with spaces
            .Trim();                // Remove leading/trailing whitespace
    }

    /// <summary>
    /// Sanitizes and truncates input to prevent overly long log entries
    /// </summary>
    /// <param name="input">The input string to sanitize</param>
    /// <param name="maxLength">Maximum allowed length (default: 100)</param>
    /// <returns>Sanitized and potentially truncated string</returns>
    public static string SanitizeAndTruncate(string? input, int maxLength = 100)
    {
        var sanitized = Sanitize(input);
        return sanitized.Length > maxLength 
            ? sanitized.Substring(0, maxLength) + "..." 
            : sanitized;
    }
}