
import React, { useState } from 'react';

// Utility functions separated for reusability and testing
const ValidationUtils = {
  validateInput: (content, mode) => {
    if (!content.trim()) {
      return 'Please enter some content to summarize';
    }
    
    if (mode === 'url') {
      const urlPattern = /^https?:\/\/.+/;
      if (!urlPattern.test(content.trim())) {
        return 'Please enter a valid URL (starting with http:// or https://)';
      }
    }
    
    return null; // No error
  }
};