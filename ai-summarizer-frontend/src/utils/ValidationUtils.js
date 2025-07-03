// Utility functions for input validation
export const ValidationUtils = {
  validateInput: (content, mode) => {
    if (!content.trim()) {
      return 'Please enter some content to summarize';
    }
    
    if (mode === 'url') {
      // Basic URL validation - check if it looks like a URL
      const urlPattern = /^https?:\/\/.+/;
      if (!urlPattern.test(content.trim())) {
        return 'Please enter a valid URL (starting with http:// or https://)';
      }
    }
    
    return null; // No error
  }
};