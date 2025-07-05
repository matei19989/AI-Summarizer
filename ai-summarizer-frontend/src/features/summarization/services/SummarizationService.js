import axios from 'axios';
import { API_CONFIG } from '../../../config/apiConfig';
import { INPUT_MODES } from '../components/constants';

export const SummarizationService = {
  /**
   * Summarizes content by calling the backend API
   * This method handles the HTTP communication between React and C# API
   * Demonstrates proper error handling and async/await patterns
   */
  async summarizeContent(content, mode) {
    try {
      // Prepare the request payload matching our C# DTO structure
      // Using PascalCase to match C# properties exactly
      const requestPayload = {
        Content: content.trim(),
        ContentType: mode === INPUT_MODES.URL ? 'url' : 'text'
      };

      console.log('Sending summarization request:', {
        url: `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.SUMMARIZE}`,
        payload: requestPayload
      });

      // Make the HTTP POST request to our C# backend
      // Using axios for better error handling and request/response interceptors
      const response = await axios.post(
        `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.SUMMARIZE}`,
        requestPayload,
        {
          headers: {
            'Content-Type': 'application/json',
          },
          timeout: API_CONFIG.TIMEOUT, // 30 second timeout
        }
      );

      console.log('Received API response:', response.data);

      // Transform the API response to match our frontend expectations
      // This abstraction layer protects the frontend from backend changes
      return {
        summary: response.data.summary,
        hasAudio: response.data.hasAudio,
        success: response.data.success,
        generatedAt: response.data.generatedAt
      };

    } catch (error) {
      console.error('API call failed:', error);
      console.error('Error details:', {
        message: error.message,
        status: error.response?.status,
        data: error.response?.data,
        headers: error.response?.headers
      });

      // Handle different types of errors gracefully
      if (error.response) {
        // Server responded with error status (4xx, 5xx)
        console.error('Server error response:', error.response.data);
        const errorMessage = error.response.data?.error || 
                           error.response.data?.Error || 
                           error.response.data?.message ||
                           `Server error: ${error.response.status}`;
        throw new Error(errorMessage);
      } else if (error.request) {
        // Request was made but no response received (network issues)
        throw new Error('Network error - please check your connection and try again');
      } else {
        // Something else happened during request setup
        throw new Error('An unexpected error occurred while processing your request');
      }
    }
  },

  /**
   * Handles audio playback for generated summaries
   * Currently a placeholder - will integrate with TTS API in future phases
   */
  playAudio(content) {
    console.log('Audio playback requested for content:', content.substring(0, 50) + '...');
    
    // TODO: Implement actual TTS integration
    // This will eventually call the backend TTS endpoint
    alert(`🔊 Audio playback will be implemented in the next phase!\n\nSummary to be spoken:\n"${content.substring(0, 100)}..."`);
  },

  /**
   * Tests the API connection - useful for debugging
   * Calls the health check endpoint to verify backend connectivity
   */
  async testConnection() {
    try {
      const response = await axios.get(
        `${API_CONFIG.BASE_URL}${API_CONFIG.ENDPOINTS.HEALTH_CHECK}`,
        { timeout: 5000 }
      );
      console.log('API health check successful:', response.data);
      return response.data;
    } catch (error) {
      console.error('API health check failed:', error);
      throw new Error('Could not connect to the backend API');
    }
  }
};