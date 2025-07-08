namespace AISummarizerAPI.Utils;

/// <summary>
/// Enhanced utility class for sanitizing user input before logging to prevent log injection attacks
/// Follows OWASP guidelines for secure logging practices
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes input by removing newline characters and other potentially dangerous characters
    /// Prevents log forging and injection attacks
    /// </summary>
    /// <param name="input">The input string to sanitize</param>
    /// <returns>Sanitized string safe for logging</returns>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return "null";

        return input
            .Replace("\r\n", " ")     // Replace Windows line endings
            .Replace("\r", " ")       // Replace carriage return
            .Replace("\n", " ")       // Replace line feed
            .Replace("\t", " ")       // Replace tabs with spaces
            .Replace("\0", "")        // Remove null characters
            .Replace("\x1B", "")      // Remove escape characters
            .Trim();                  // Remove leading/trailing whitespace
    }

    /// <summary>
    /// Sanitizes and truncates input to prevent overly long log entries
    /// </summary>
    /// <param name="input">The input string to sanitize</param>
    /// <param name="maxLength">Maximum allowed length (default: 200)</param>
    /// <returns>Sanitized and potentially truncated string</returns>
    public static string SanitizeAndTruncate(string? input, int maxLength = 200)
    {
        var sanitized = Sanitize(input);
        return sanitized.Length > maxLength
            ? sanitized.Substring(0, maxLength) + "..."
            : sanitized;
    }

    /// <summary>
    /// Sanitizes URLs specifically for logging, handling common URL edge cases
    /// </summary>
    /// <param name="url">The URL to sanitize</param>
    /// <returns>Sanitized URL safe for logging</returns>
    public static string SanitizeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return "null";

        var sanitized = Sanitize(url);

        // Truncate very long URLs but preserve the domain for debugging
        if (sanitized.Length > 200)
        {
            try
            {
                var uri = new Uri(sanitized);
                var domain = uri.Host;
                return $"{domain}/... (truncated, original length: {sanitized.Length})";
            }
            catch
            {
                // If URL parsing fails, just truncate normally
                return sanitized.Substring(0, 200) + "...";
            }
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes content type strings for logging
    /// </summary>
    /// <param name="contentType">The content type to sanitize</param>
    /// <returns>Sanitized content type safe for logging</returns>
    public static string SanitizeContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return "unknown";

        var sanitized = Sanitize(contentType);

        // Content types should be short, but just in case
        return sanitized.Length > 50
            ? sanitized.Substring(0, 50) + "..."
            : sanitized;
    }
}